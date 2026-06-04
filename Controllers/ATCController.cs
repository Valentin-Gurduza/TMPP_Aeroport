using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TMPP_Aeroport.Data;
using Microsoft.AspNetCore.SignalR;
using TMPP_Aeroport.Hubs;
using TMPP_Aeroport.Services;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using TMPP_Aeroport.Domain.Mediator;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin,ATC_Manager")]
    public class ATCController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly FlightSimulationService _flightSimulation;
        private readonly IHubContext<FlightHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IATCMediator _tower;
        private readonly TMPP_Aeroport.Domain.Flyweight.AircraftModelFactory _aircraftModelFactory;

        public ATCController(ApplicationDbContext dbContext, FlightSimulationService flightSimulation, IHubContext<FlightHub> hubContext, IServiceScopeFactory scopeFactory, IATCMediator tower, TMPP_Aeroport.Domain.Flyweight.AircraftModelFactory aircraftModelFactory)
        {
            _dbContext = dbContext;
            _flightSimulation = flightSimulation;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _tower = tower;
            _aircraftModelFactory = aircraftModelFactory;
        }

        // Live Radar Simulation
        public IActionResult Radar()
        {
            ViewBag.CurrentSpeed = TMPP_Aeroport.Services.FlightSimulationService.GlobalSpeedMultiplier;
            return View();
        }

        // Flyweight Pattern: Radar Data API Endpoint
        [HttpGet]
        public async Task<IActionResult> RadarData()
        {
            var activeFlights = _flightSimulation.GetActiveFlights()
                .Where(f => f.Status == "Airborne" || f.Status.Contains("Holding"));

            var factory = _aircraftModelFactory;
            var blips = new List<object>();

            // The DB context is just to get aircraft model names since we only have FlightNumber in Simulation Service
            // Wait, I can fetch the flights from DB once
            var flightNumbers = activeFlights.Select(f => f.FlightNumber).ToList();
            var dbFlights = await _dbContext.Flights.Include(f => f.Aircraft)
                .Where(f => flightNumbers.Contains(f.FlightNumber))
                .ToListAsync();

            foreach (var simFlight in activeFlights)
            {
                var dbFlight = dbFlights.FirstOrDefault(f => f.FlightNumber == simFlight.FlightNumber);
                string modelName = dbFlight?.Aircraft?.Model ?? "Generic Plane";

                // AircraftModelData is created ONCE per model type, saving RAM on server
                var sharedModel = factory.GetAircraftModel(modelName);

                // RadarBlip is extremely lightweight, uses the shared Model
                var blip = new TMPP_Aeroport.Domain.Flyweight.RadarBlip(
                    simFlight.FlightNumber, 
                    simFlight.CurrentLat, 
                    simFlight.CurrentLng, 
                    sharedModel
                );

                blips.Add(new {
                    flightNumber = blip.FlightNumber,
                    lat = blip.Latitude,
                    lng = blip.Longitude,
                    modelType = blip.GetSharedModelName(), // Client UI will use this to select correct texture/sprite
                    status = simFlight.Status,
                    origin = dbFlight?.Origin ?? "UNK",
                    destination = dbFlight?.Destination ?? "UNK",
                    originLat = simFlight.OriginLat,
                    originLng = simFlight.OriginLng,
                    destLat = simFlight.DestLat,
                    destLng = simFlight.DestLng
                });
            }

            return Json(new { 
                cacheSize = factory.GetCacheSize(), 
                blips = blips 
            });
        }

        // Mediator Pattern: ATC Tower
        [HttpGet]
        public IActionResult ATCTower()
        {
            var flights = _dbContext.Flights.Where(f => f.Status == "Airborne" || f.Status == "Scheduled" || f.Status.Contains("Boarding") || f.Status.Contains("Awaiting")).ToList();
            
            var concreteTower = _tower as TMPP_Aeroport.Domain.Mediator.ATCTower;
            ViewBag.TowerLogs = concreteTower?.ATCLogs ?? new List<string>();
            ViewBag.Flights = flights;
            ViewBag.PendingRequests = _flightSimulation.GetPendingRequests();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendChatMessage([FromForm] string senderFlightNumber, [FromForm] string message, [FromForm] string actionType)
        {
            if (!string.IsNullOrEmpty(senderFlightNumber))
            {
                var sender = new TMPP_Aeroport.Domain.Mediator.CommercialFlight(_tower, senderFlightNumber);
                
                if (actionType == "Landing")
                {
                    sender.RequestLanding();
                    await _hubContext.Clients.All.SendAsync("ReceiveATCChat", senderFlightNumber, "Requesting Landing Clearance", "Aircraft");
                    // Tower auto-responds
                    await Task.Delay(500);
                    await _hubContext.Clients.All.SendAsync("ReceiveATCChat", "TOWER", $"[TOWER] {senderFlightNumber}, cleared for landing approach.", "System");
                }
                else if (!string.IsNullOrEmpty(message))
                {
                    sender.Send(message);
                    await _hubContext.Clients.All.SendAsync("ReceiveATCChat", senderFlightNumber, message, "Aircraft");
                }
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HandleTowerCommand([FromForm] string commandType, [FromForm] string commandFlight)
        {
            if (!string.IsNullOrEmpty(commandType) && !string.IsNullOrEmpty(commandFlight))
            {
                // Proxy Integration: Check security before granting airspace commands
                if (commandType == "ClearTakeoff" || commandType == "ClearLanding")
                {
                    var role = User.IsInRole("Admin") ? "Admin" : (User.IsInRole("ATC_Manager") ? "ATC_Manager" : "Staff");
                    var proxy = new TMPP_Aeroport.Domain.Proxy.RunwayControlProxy(role);
                    string result = proxy.GrantClearance(commandFlight, "MAIN RUNWAY");
                    
                    if (result.Contains("ACCESS DENIED"))
                    {
                        TempData["ErrorMessage"] = $"Security Proxy blocked command: {result}";
                        return RedirectToAction("ATCTower");
                    }
                }

                if (commandType == "ClearTakeoff") _flightSimulation.ApproveTakeoff(commandFlight);
                else if (commandType == "ClearLanding") _flightSimulation.ApproveLanding(commandFlight);
                else if (commandType == "ReturnBase") _flightSimulation.ReturnToOrigin(commandFlight);
                else if (commandType == "Divert") _flightSimulation.DivertToNearest(commandFlight);
                
                TempData["SuccessMessage"] = $"Command {commandType} executed for {commandFlight}.";
            }
            return RedirectToAction("ATCTower");
        }

        // Observer Pattern: Global Alerts
        [HttpPost]
        public async Task<IActionResult> AlertsExecute(string flightNumber, string newStatus)
        {
            if (string.IsNullOrEmpty(flightNumber)) return BadRequest("Flight number is required.");

            var flight = await _dbContext.Flights.FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            if (flight == null) return BadRequest("Flight not found.");

            string oldStatus = "Unknown";
            string origin = "N/A";
            string destination = "N/A";

            if (flight != null)
            {
                oldStatus = flight.Status;
                origin = flight.Origin;
                destination = flight.Destination;

                if (!TMPP_Aeroport.Domain.AirportConfig.IsValidTransition(oldStatus, newStatus))
                {
                    ViewBag.Error = $"Tranziția din starea {oldStatus} în {newStatus} nu este permisă.";
                    return await RenderAlertsView();
                }

                flight.Status = newStatus;
                await _dbContext.SaveChangesAsync();
            }

            try
            {
                var subject = new TMPP_Aeroport.Domain.Observer.FlightStatusSubject(flightNumber);

                subject.Attach(new TMPP_Aeroport.Domain.Observer.PassengerNotifier(_dbContext));
                subject.Attach(new TMPP_Aeroport.Domain.Observer.DisplayBoardUpdater(_dbContext));

                subject.Status = newStatus;
                
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Soft fail for notifications, log system error
                ViewBag.Error = "Starea a fost actualizată, dar notificările au întâmpinat o eroare: " + ex.Message;
            }
            var logs = await _dbContext.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(4)
                .Select(a => a.Message)
                .ToListAsync();

            await _hubContext.Clients.All.SendAsync("FlightStateChanged", new {
                FlightId = flight?.Id ?? 0,
                FlightNumber = flightNumber,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Origin = origin,
                Destination = destination
            });

            ViewBag.Logs = logs;
            ViewBag.FlightNumber = flightNumber;
            ViewBag.NewStatus = newStatus;
            return await RenderAlertsView();
        }

        [HttpGet]
        public async Task<IActionResult> Alerts()
        {
            return await RenderAlertsView();
        }

        private async Task<IActionResult> RenderAlertsView()
        {
            ViewBag.Flights = await _dbContext.Flights.ToListAsync();
            ViewBag.StatusOptions = TMPP_Aeroport.Domain.AirportConfig.StatusOptions;
            return View("Alerts");
        }

        // Command Pattern: Runway Lights
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Command.RunwayReceiver> _receivers = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Command.AtcInvoker> _invokers = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _lightsOnStates = new();

        private string GetSessionKey() 
        {
            if (_receivers.Count > 100 || _lightsOnStates.Count > 100)
            {
                _receivers.Clear();
                _invokers.Clear();
                _lightsOnStates.Clear();
            }
            return (User.Identity?.IsAuthenticated == true ? User.Identity.Name : null) ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        public async Task<IActionResult> RunwayLights(string commandName)
        {
            var key = GetSessionKey();
            var receiver = _receivers.GetOrAdd(key, _ => new TMPP_Aeroport.Domain.Command.RunwayReceiver(_scopeFactory));
            var invoker = _invokers.GetOrAdd(key, _ => new TMPP_Aeroport.Domain.Command.AtcInvoker());

            if (!string.IsNullOrEmpty(commandName))
            {
                if (commandName == "Undo")
                {
                    invoker.UndoLastCommand();
                }
                else
                {
                    var factory = new TMPP_Aeroport.Domain.Command.RunwayCommandFactory();
                    var command = factory.CreateCommand(commandName, receiver);
                    invoker.ExecuteCommand(command);

                    if (commandName == "Emergency")
                    {
                        // Broadcast the emergency alert via SignalR
                        await _hubContext.Clients.All.SendAsync("FlightStateChanged", new {
                            FlightNumber = "SYSTEM",
                            OldStatus = "Normal",
                            NewStatus = "EMERGENCY PROTOCOL ACTIVE",
                            Origin = "ATC",
                            Destination = "ALL"
                        });
                    }
                }

                _lightsOnStates[key] = receiver.AreLightsOn;
                await _hubContext.Clients.All.SendAsync("RunwayLights", receiver.AreLightsOn);
            }

            ViewBag.Logs = receiver.Logs;
            ViewBag.LightsOn = receiver.AreLightsOn;
            return View();
        }
    }
}
