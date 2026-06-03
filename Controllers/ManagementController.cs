using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

using TMPP_Aeroport.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> NukeGhostFlights()
        {
            try 
            {
                var badFlights = await _context.Flights.Where(f => f.Status != "Completed").ToListAsync();
                foreach(var f in badFlights) f.Status = "Completed";
                await _context.SaveChangesAsync();
                
                if (System.IO.File.Exists("simulation_savegame.json"))
                {
                    System.IO.File.Delete("simulation_savegame.json");
                }
                
                return Json(new { success = true, nukedCount = badFlights.Count });
            } 
            catch(Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Prototype & Abstract Factory: Flight Scheduler UI
        public async Task<IActionResult> FlightScheduler(int? pageNumber)
        {
            var flights = _context.Flights.Include(f => f.Aircraft).AsNoTracking().OrderBy(f => f.DepartureTime);
            int pageSize = 10;
            return View(await TMPP_Aeroport.Models.PaginatedList<TMPP_Aeroport.Models.Flight>.CreateAsync(flights, pageNumber ?? 1, pageSize));
        }



        // --- Memento Pattern: Edit Flight Config & Undo ---
        private static readonly Dictionary<int, TMPP_Aeroport.Domain.Memento.FlightConfigHistory> _mementoHistories = new Dictionary<int, TMPP_Aeroport.Domain.Memento.FlightConfigHistory>();

        [HttpPost]
        public IActionResult EditFlightConfig(int flightId, string newGate, DateTime newTime)
        {
            var flight = _context.Flights.FirstOrDefault(f => f.Id == flightId);
            if (flight == null) return Json(new { success = false });

            // Create history stack if not exists
            if (!_mementoHistories.ContainsKey(flightId))
                _mementoHistories[flightId] = new TMPP_Aeroport.Domain.Memento.FlightConfigHistory();

            // Setup Originator
            var originator = new TMPP_Aeroport.Domain.Memento.FlightConfigurator();
            originator.SetConfiguration(flight.Gate ?? "TBA", flight.DepartureTime, flight.Aircraft?.Model ?? "Generic");
            
            // Backup current state
            _mementoHistories[flightId].Backup(originator);

            // Apply new state
            flight.Gate = newGate;
            flight.DepartureTime = newTime;
            _context.SaveChanges();

            return Json(new { success = true, message = "Flight configuration updated. Memento saved for Undo." });
        }

        [HttpPost]
        public IActionResult UndoFlightConfig(int flightId)
        {
            var flight = _context.Flights.Include(f => f.Aircraft).FirstOrDefault(f => f.Id == flightId);
            if (flight == null || !_mementoHistories.ContainsKey(flightId)) return Json(new { success = false, message = "No history available." });

            var originator = new TMPP_Aeroport.Domain.Memento.FlightConfigurator();
            _mementoHistories[flightId].Undo(originator);

            if (string.IsNullOrEmpty(originator.Gate) && originator.DepartureTime == DateTime.MinValue)
            {
                 return Json(new { success = false, message = "No further undo steps available." });
            }

            flight.Gate = originator.Gate;
            flight.DepartureTime = originator.DepartureTime;
            _context.SaveChanges();

            return Json(new { success = true, message = "Flight configuration restored to previous state.", newGate = flight.Gate, newTime = flight.DepartureTime.ToString("yyyy-MM-ddTHH:mm:ss") });
        }

        [HttpPost]
        public async Task<IActionResult> CloneFlight(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return Json(new { success = false, message = "Flight not found." });
            }

            // Prototype Pattern usage
            var clonedFlight = (TMPP_Aeroport.Models.Flight)flight.Clone();
            
            // Adjust details for the cloned flight
            clonedFlight.DepartureTime = clonedFlight.DepartureTime.AddDays(1); // clone it for the next day
            clonedFlight.Status = "Scheduled";
            clonedFlight.FlightNumber = clonedFlight.FlightNumber + "-C"; // mark as clone just to be safe
            
            _context.Flights.Add(clonedFlight);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Flight {flight.FlightNumber} cloned successfully for {clonedFlight.DepartureTime:yyyy-MM-dd}." });
        }



        // Singleton: System Audit Logs
        public IActionResult AuditLogs()
        {
            var logs = _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .Select(l => $"[{l.Timestamp:yyyy-MM-dd HH:mm:ss}] {l.Message}")
                .ToList();
                
            return View(logs);
        }

        private async Task<List<TMPP_Aeroport.Domain.Visitor.IAirportElement>> GetRealElementsAsync()
        {
            var elements = new List<TMPP_Aeroport.Domain.Visitor.IAirportElement>();
            
            // Terminals
            var flights = await _context.Flights.ToListAsync();
            var terminalGroups = flights.GroupBy(f => f.Terminal);
            foreach (var group in terminalGroups)
            {
                int distinctGates = group.Select(f => f.Gate).Distinct().Count();
                elements.Add(new TMPP_Aeroport.Domain.Visitor.TerminalElement(group.Key ?? "Unknown Terminal", distinctGates));
            }

            // Aircrafts
            var aircrafts = await _context.Aircrafts.ToListAsync();
            foreach (var aircraft in aircrafts)
            {
                elements.Add(new TMPP_Aeroport.Domain.Visitor.AircraftElement(aircraft.Model ?? "Unknown Model", aircraft.Capacity));
            }

            // Flights
            foreach (var flight in flights)
            {
                elements.Add(new TMPP_Aeroport.Domain.Visitor.FlightElement(flight.Id, flight.FlightNumber, flight.Destination ?? "Unknown", flight.DepartureTime));
            }

            return elements;
        }

        // Visitor: Analytics & Export
        public async Task<IActionResult> Analytics(string format = "json")
        {
            var elements = await GetRealElementsAsync();

            TMPP_Aeroport.Domain.Visitor.IVisitor visitor;
            if (format == "xml") visitor = new TMPP_Aeroport.Domain.Visitor.XmlExportVisitor();
            else visitor = new TMPP_Aeroport.Domain.Visitor.JsonExportVisitor();

            foreach (var element in elements)
            {
                element.Accept(visitor);
            }

            if (format == "xml")
                ViewBag.ExportData = ((TMPP_Aeroport.Domain.Visitor.XmlExportVisitor)visitor).ExportedData;
            else
                ViewBag.ExportData = ((TMPP_Aeroport.Domain.Visitor.JsonExportVisitor)visitor).ExportedData;

            ViewBag.Format = format;
            return View();
        }

        public async Task<IActionResult> DownloadAnalytics(string format = "json")
        {
            var elements = await GetRealElementsAsync();

            string exportData = "";
            string contentType = "application/json";
            string fileName = "airport_export.json";

            if (format == "xml")
            {
                var visitor = new TMPP_Aeroport.Domain.Visitor.XmlExportVisitor();
                foreach (var element in elements) element.Accept(visitor);
                exportData = string.Join(Environment.NewLine, visitor.ExportedData);
                contentType = "application/xml";
                fileName = "airport_export.xml";
            }
            else
            {
                var visitor = new TMPP_Aeroport.Domain.Visitor.JsonExportVisitor();
                foreach (var element in elements) element.Accept(visitor);
                exportData = string.Join(Environment.NewLine, visitor.ExportedData);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(exportData);
            return File(bytes, contentType, fileName);
        }

        // Visitor Pattern: Apply Delay to all Flights
        [HttpPost]
        public async Task<IActionResult> DelayAllFlights(int hoursDelay)
        {
            if (hoursDelay <= 0) return Json(new { success = false, message = "Delay must be greater than 0." });

            var elements = await GetRealElementsAsync();
            var delayVisitor = new TMPP_Aeroport.Domain.Visitor.FlightDelayVisitor(TimeSpan.FromHours(hoursDelay));

            // Visit all elements
            foreach (var element in elements)
            {
                element.Accept(delayVisitor);
            }

            // Apply delays to the database using a single query
            var delayedIds = delayVisitor.DelayedFlights.Select(d => d.Id).ToList();
            var flightsToUpdate = await _context.Flights.Where(f => delayedIds.Contains(f.Id)).ToListAsync();

            foreach (var flight in flightsToUpdate)
            {
                var delayedData = delayVisitor.DelayedFlights.FirstOrDefault(d => d.Id == flight.Id);
                if (delayedData != null)
                {
                    flight.DepartureTime = delayedData.DepartureTime;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Successfully delayed {delayVisitor.DelayedFlights.Count} flights by {hoursDelay} hours." });
        }
    }
}
