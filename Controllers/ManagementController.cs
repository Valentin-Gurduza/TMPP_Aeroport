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

        // Prototype & Abstract Factory: Flight Scheduler UI
        public IActionResult FlightScheduler()
        {
            var flights = _context.Flights.Include(f => f.Aircraft).ToList();
            return View(flights);
        }

        // AJAX API: Clone Flight
        [HttpPost]
        public IActionResult CloneFlight(int id)
        {
            var original = _context.Flights.FirstOrDefault(f => f.Id == id);
            if (original != null)
            {
                // Bug Fix: Prototype Pattern now uses ICloneable correctly
                var clone = (TMPP_Aeroport.Models.Flight)original.Clone();
                clone.FlightNumber = original.FlightNumber + "-C";
                clone.DepartureTime = original.DepartureTime.AddDays(1);
                clone.Status = TMPP_Aeroport.Models.FlightStatus.Draft;
                _context.Flights.Add(clone);
                _context.SaveChanges();
                
                return Json(new { success = true, clonedFlight = clone.FlightNumber });
            }
            return Json(new { success = false });
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
            // Originator needs current state if we want to log it, but undo overwrites it anyway.
            // Just call Undo which will restore the originator to the previous state.
            _mementoHistories[flightId].Undo(originator);

            if (string.IsNullOrEmpty(originator.Gate) && originator.DepartureTime == DateTime.MinValue)
            {
                 // Stack was empty or undo failed
                 return Json(new { success = false, message = "No further undo steps available." });
            }

            // Apply restored state to DB
            flight.Gate = originator.Gate;
            flight.DepartureTime = originator.DepartureTime;
            _context.SaveChanges();

            return Json(new { success = true, message = "Flight configuration restored to previous state.", newGate = flight.Gate, newTime = flight.DepartureTime.ToString("yyyy-MM-ddTHH:mm:ss") });
        }

        // AJAX API: Generate Document
        [HttpGet]
        public IActionResult GenerateDocument(string type)
        {
            TMPP_Aeroport.Domain.AbstractFactory.IFlightDocumentFactory factory;
            if (type.ToLower() == "business")
                factory = new TMPP_Aeroport.Domain.AbstractFactory.BusinessDocumentFactory();
            else
                factory = new TMPP_Aeroport.Domain.AbstractFactory.EconomyDocumentFactory();

            var boardingPass = factory.CreateBoardingPass();
            var baggageTag = factory.CreateBaggageTag();
            
            boardingPass.PassengerName = "Sample Passenger";
            boardingPass.FlightNumber = "GENERIC-001";
            baggageTag.Code = "BGG-SAMPLE";

            return Json(new { 
                success = true, 
                ticketType = type,
                passenger = boardingPass.PassengerName,
                flight = boardingPass.FlightNumber,
                details = boardingPass.GetTicketDetails(),
                tagCode = baggageTag.Code,
                tagColor = baggageTag.GetTagColor()
            });
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

            // Apply delays to the database
            foreach (var delayedFlight in delayVisitor.DelayedFlights)
            {
                var flight = await _context.Flights.FindAsync(delayedFlight.Id);
                if (flight != null)
                {
                    flight.DepartureTime = delayedFlight.DepartureTime; // This is the updated time
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Successfully delayed {delayVisitor.DelayedFlights.Count} flights by {hoursDelay} hours." });
        }
    }
}
