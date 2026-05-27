using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TMPP_Aeroport.Data;
using System.Linq;
using System;
using TMPP_Aeroport.Domain.Composite;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin,Ground_Staff")]
    public class GroundOpsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public GroundOpsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Facade & Template Method: Pre-Flight Checks
        [HttpPost]
        public async Task<IActionResult> PreFlightChecksExecute(string flightNumber, string runway, string flightType)
        {
            // Template Method execution
            TMPP_Aeroport.Domain.TemplateMethod.FlightPreflightRoutine routine;
            if (flightType == "cargo") routine = new TMPP_Aeroport.Domain.TemplateMethod.CargoFlightRoutine();
            else routine = new TMPP_Aeroport.Domain.TemplateMethod.PassengerFlightRoutine();
            
            routine.ExecuteRoutine();

            // Facade execution
            var facade = new TMPP_Aeroport.Domain.Facade.FlightDepartureFacade();
            var flightLog = facade.AuthoriseDeparture(flightNumber, runway);

            var flight = await _dbContext.Flights.FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            if (flight != null)
            {
                // Simulate ground ops completing checks. Status is moved to Awaiting Takeoff Clearance
                // But realistically, Ground Ops prepares the flight for ATC.
                // We just log it for now.
            }

            ViewBag.FlightLog = flightLog;
            ViewBag.RoutineLogs = routine.RoutineLogs;
            ViewBag.FlightNumber = flightNumber;
            ViewBag.Flights = await _dbContext.Flights.Where(f => f.Status == "Scheduled" || f.Status == "Boarding").ToListAsync();
            
            return View("PreFlightChecks");
        }

        [HttpGet]
        public async Task<IActionResult> PreFlightChecks()
        {
            ViewBag.Flights = await _dbContext.Flights.Where(f => f.Status == "Scheduled" || f.Status == "Boarding").ToListAsync();
            return View();
        }

        // Bridge Pattern: Terminal Displays
        public IActionResult TerminalDisplays(string hardware = "led")
        {
            var flights = _dbContext.Flights.OrderBy(f => f.DepartureTime).Take(6).ToList();

            TMPP_Aeroport.Domain.Bridge.IDisplayRenderer renderer = (hardware == "web") ? 
                new TMPP_Aeroport.Domain.Bridge.WebRenderer() : 
                new TMPP_Aeroport.Domain.Bridge.LEDRenderer();

            TMPP_Aeroport.Domain.Bridge.FlightBoard departures = new TMPP_Aeroport.Domain.Bridge.DeparturesBoard(renderer);
            TMPP_Aeroport.Domain.Bridge.FlightBoard arrivals = new TMPP_Aeroport.Domain.Bridge.ArrivalsBoard(renderer);

            ViewBag.Hardware = hardware;
            ViewBag.DeparturesRender = departures.ShowBoard(flights);
            ViewBag.ArrivalsRender = arrivals.ShowBoard(flights);

            return View();
        }

        // Memento Pattern: Gate Assignments
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Memento.FlightConfigurator> _originators = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Memento.FlightConfigHistory> _caretakers = new();

        private string GetSessionKey() => (User.Identity?.IsAuthenticated == true ? User.Identity.Name : null) ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        public IActionResult GateAssignments(string actionType, string newGate, string newModel)
        {
            var key = GetSessionKey();
            var originator = _originators.GetOrAdd(key, _ => new TMPP_Aeroport.Domain.Memento.FlightConfigurator() { Gate = "A1", DepartureTime = DateTime.Now.AddHours(2), AircraftModel = "Boeing 737" });
            var caretaker = _caretakers.GetOrAdd(key, _ => new TMPP_Aeroport.Domain.Memento.FlightConfigHistory());

            if (actionType == "Save")
            {
                caretaker.Backup(originator);
            }
            else if (actionType == "Update")
            {
                originator.SetConfiguration(newGate ?? "A1", DateTime.Now.AddHours(3), newModel ?? "Airbus A320");
            }
            else if (actionType == "Undo")
            {
                caretaker.Undo(originator);
            }

            ViewBag.CurrentGate = originator.Gate;
            ViewBag.CurrentModel = originator.AircraftModel;
            ViewBag.Logs = originator.ActionLogs;

            return View();
        }

        // Composite Pattern: Cargo Manifest
        public IActionResult CargoManifest()
        {
            ILuggageItem pasager1Valiza1 = new Suitcase("Valiză Roșie", 23.5);
            ILuggageItem pasager1Rucsac = new Backpack("Rucsac Laptop", 5.2);
            ILuggageItem pasager2Valiza = new Suitcase("Valiză Neagră", 18.0);

            LuggageContainer popescuFamilyContainer = new LuggageContainer("POP-01");
            popescuFamilyContainer.Add(pasager1Valiza1);
            popescuFamilyContainer.Add(pasager1Rucsac);
            popescuFamilyContainer.Add(pasager2Valiza);

            ILuggageItem pasager3Valiza = new Suitcase("Geantă Golf", 12.0);

            LuggageContainer mainCargo = new LuggageContainer("CARGO-MAIN-737");
            mainCargo.Add(popescuFamilyContainer);
            mainCargo.Add(pasager3Valiza);

            ViewBag.MainCargo = mainCargo;
            ViewBag.TotalWeight = mainCargo.GetWeight();
            ViewBag.StringTree = mainCargo.Display();

            return View();
        }
    }
}
