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
        private readonly TMPP_Aeroport.Services.FlightSimulationService _simService;

        public GroundOpsController(ApplicationDbContext dbContext, TMPP_Aeroport.Services.FlightSimulationService simService)
        {
            _dbContext = dbContext;
            _simService = simService;
        }

        // Facade & Template Method: Pre-Flight Checks
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreFlightChecksExecute(string flightNumber, string runway, string flightType)
        {
            var flight = await _dbContext.Flights.Include(f => f.Aircraft).FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            if (flight == null)
            {
                ViewBag.Error = "Zborul nu a fost găsit.";
                return await RenderPreFlightView();
            }

            if (!TMPP_Aeroport.Domain.AirportConfig.Runways.Any(r => r.Code == runway && r.Status == "Available"))
            {
                ViewBag.Error = $"Pista {runway} nu este disponibilă sau nu există.";
                return await RenderPreFlightView();
            }

            // Template Method execution
            TMPP_Aeroport.Domain.TemplateMethod.FlightPreflightRoutine routine;
            if (flightType == "cargo" || flight.Aircraft?.Type == "Cargo") 
                routine = new TMPP_Aeroport.Domain.TemplateMethod.CargoFlightRoutine(_dbContext, flight);
            else 
                routine = new TMPP_Aeroport.Domain.TemplateMethod.PassengerFlightRoutine(_dbContext, flight);
            
            routine.ExecuteRoutine();

            List<string> flightLog = new List<string>();

            if (routine.IsSuccessful)
            {
                // Facade execution
                var facade = new TMPP_Aeroport.Domain.Facade.FlightDepartureFacade(_dbContext);
                var facadeResult = facade.AuthoriseDeparture(flightNumber, runway);
                flightLog = facadeResult.Logs;
            }
            else
            {
                flightLog.Add("❌ Decolarea a fost oprită deoarece verificările pre-zbor au eșuat.");
            }

            ViewBag.FlightLog = flightLog;
            ViewBag.RoutineLogs = routine.RoutineLogs;
            ViewBag.FlightNumber = flightNumber;
            ViewBag.FlightLog = flightLog;
            ViewBag.RoutineLogs = routine.RoutineLogs;
            ViewBag.FlightNumber = flightNumber;
            
            return await RenderPreFlightView("PreFlightChecks");
        }

        [HttpGet]
        public async Task<IActionResult> PreFlightChecks()
        {
            return await RenderPreFlightView();
        }

        private async Task<IActionResult> RenderPreFlightView(string viewName = "PreFlightChecks")
        {
            ViewBag.Runways = TMPP_Aeroport.Domain.AirportConfig.Runways.Where(r => r.Status == "Available").ToList();
            ViewBag.FlightTypes = TMPP_Aeroport.Domain.AirportConfig.FlightTypes;
            ViewBag.Flights = await _dbContext.Flights.Where(f => f.Status == "Scheduled" || f.Status == "Boarding").ToListAsync();
            return View(viewName);
        }



        // Memento Pattern: Gate Assignments
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Memento.FlightConfigurator> _originators = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Memento.FlightConfigHistory> _caretakers = new();

        private string GetSessionKey() 
        {
            if (_originators.Count > 100 || _caretakers.Count > 100)
            {
                _originators.Clear();
                _caretakers.Clear();
            }
            return (User.Identity?.IsAuthenticated == true ? User.Identity.Name : null) ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        [HttpGet]
        public async Task<IActionResult> GateAssignments(int? flightId)
        {
            var key = GetSessionKey();
            
            // Populate Dropdown
            var flights = await _dbContext.Flights.Where(f => f.Status == "Scheduled" || f.Status == "Boarding" || f.Status == "Draft").ToListAsync();
            ViewBag.Flights = flights;

            if (flightId.HasValue)
            {
                var flight = await _dbContext.Flights.Include(f => f.Aircraft).FirstOrDefaultAsync(f => f.Id == flightId.Value);
                if (flight != null)
                {
                    var originator = _originators.GetOrAdd(key + "_" + flight.Id, _ => new TMPP_Aeroport.Domain.Memento.FlightConfigurator() 
                    { 
                        Gate = flight.Gate ?? "Unassigned", 
                        DepartureTime = flight.DepartureTime, 
                        AircraftModel = flight.Aircraft?.Model ?? "Unknown" 
                    });
                    var caretaker = _caretakers.GetOrAdd(key + "_" + flight.Id, _ => new TMPP_Aeroport.Domain.Memento.FlightConfigHistory());

                    ViewBag.CurrentGate = originator.Gate;
                    ViewBag.CurrentModel = originator.AircraftModel;
                    ViewBag.Logs = originator.ActionLogs;
                    ViewBag.SelectedFlightId = flight.Id;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GateAssignmentsExecute(int flightId, string actionType, string newGate)
        {
            var key = GetSessionKey();
            var flight = await _dbContext.Flights.Include(f => f.Aircraft).FirstOrDefaultAsync(f => f.Id == flightId);
            if (flight != null)
            {
                var originator = _originators.GetOrAdd(key + "_" + flight.Id, _ => new TMPP_Aeroport.Domain.Memento.FlightConfigurator() 
                { 
                    Gate = flight.Gate ?? "Unassigned", 
                    DepartureTime = flight.DepartureTime, 
                    AircraftModel = flight.Aircraft?.Model ?? "Unknown" 
                });
                var caretaker = _caretakers.GetOrAdd(key + "_" + flight.Id, _ => new TMPP_Aeroport.Domain.Memento.FlightConfigHistory());

                if (actionType == "Save")
                {
                    caretaker.Backup(originator);
                    // Save to real DB
                    flight.Gate = originator.Gate;
                    await _dbContext.SaveChangesAsync();
                    originator.ActionLogs.Add($"Saved gate {originator.Gate} to Database for flight {flight.FlightNumber}");
                }
                else if (actionType == "Update" && !string.IsNullOrEmpty(newGate))
                {
                    originator.SetConfiguration(newGate, originator.DepartureTime, originator.AircraftModel);
                }
                else if (actionType == "Undo")
                {
                    caretaker.Undo(originator);
                    // Revert in real DB if necessary
                    flight.Gate = originator.Gate;
                    await _dbContext.SaveChangesAsync();
                }
            }
            return RedirectToAction("GateAssignments", new { flightId = flightId });
        }

        // Composite Pattern: Cargo Manifest
        public async Task<IActionResult> CargoManifest(string flightNumberFilter = "")
        {
            var dbBaggageQuery = _dbContext.BaggageItems.Include(b => b.Flight).AsQueryable();
            
            if (!string.IsNullOrEmpty(flightNumberFilter))
            {
                dbBaggageQuery = dbBaggageQuery.Where(b => b.Flight != null && b.Flight.FlightNumber == flightNumberFilter);
            }

            var dbBaggage = await dbBaggageQuery.ToListAsync();
            
            LuggageContainer mainCargo = new LuggageContainer("AIRPORT-MAIN-HUB");

            // Group by flight
            var flightGroups = dbBaggage.GroupBy(b => b.Flight?.FlightNumber ?? "UNK");

            foreach (var group in flightGroups)
            {
                LuggageContainer flightContainer = new LuggageContainer($"FLIGHT-{group.Key}");
                
                foreach (var item in group)
                {
                    ILuggageItem luggageItem;
                    if (item.Type == "Backpack") 
                        luggageItem = new Backpack($"Tag: {item.TagCode}", item.Weight);
                    else 
                        luggageItem = new Suitcase($"Tag: {item.TagCode}", item.Weight);
                        
                    flightContainer.Add(luggageItem);
                }
                
                mainCargo.Add(flightContainer);
            }

            // We want to pass the hierarchical object to the View
            ViewBag.MainCargo = mainCargo;
            ViewBag.TotalWeight = mainCargo.GetWeight();
            
            // Generate list of flights for the dropdown filter
            ViewBag.Flights = await _dbContext.Flights.Where(f => f.Status == "Scheduled" || f.Status == "Boarding").ToListAsync();
            ViewBag.FlightNumberFilter = flightNumberFilter;

            return View();
        }

        // Feature 1: BHS - Baggage Routing Live View
        [HttpGet]
        public async Task<IActionResult> BaggageRouting()
        {
            var stages = new[] { "PendingCheckIn", "CheckedIn", "OnConveyor", "XRayScreening", "Sorted", "LoadedOnAircraft" };
            var bags = await _dbContext.BaggageItems.Include(b => b.Flight).ToListAsync();
            ViewBag.Stages = stages;
            ViewBag.Bags = bags;
            return View();
        }

        // Feature 2: GSE - Ground Support Equipment Live View
        [HttpGet]
        public IActionResult GroundVehicles()
        {
            var pool = _simService.GetVehiclePool();
            ViewBag.Vehicles = pool.AllVehicles;
            ViewBag.ActiveFlights = _simService.GetActiveFlights().Where(f => f.Status == "Landed" || f.ServicingStarted).ToList();
            return View();
        }
    }
}
