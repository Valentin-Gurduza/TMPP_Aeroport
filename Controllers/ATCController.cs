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

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin,ATC_Manager")]
    public class ATCController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly FlightSimulationService _flightSimulation;
        private readonly IHubContext<FlightHub> _hubContext;

        public ATCController(ApplicationDbContext dbContext, FlightSimulationService flightSimulation, IHubContext<FlightHub> hubContext)
        {
            _dbContext = dbContext;
            _flightSimulation = flightSimulation;
            _hubContext = hubContext;
        }

        // Live Radar Simulation
        public IActionResult Radar()
        {
            return View();
        }

        // Mediator Pattern: ATC Tower
        public IActionResult ATCTower(string senderFlightNumber, string message, string actionType, string commandType, string commandFlight)
        {
            var tower = new TMPP_Aeroport.Domain.Mediator.ATCTower();
            var flights = _dbContext.Flights.Where(f => f.Status == "Airborne" || f.Status == "Scheduled" || f.Status.Contains("Boarding") || f.Status.Contains("Awaiting")).ToList();
            
            // --- Process ATC Commands (Strict simulation overrides) ---
            if (!string.IsNullOrEmpty(commandType) && !string.IsNullOrEmpty(commandFlight))
            {
                if (commandType == "ClearTakeoff") _flightSimulation.ApproveTakeoff(commandFlight);
                else if (commandType == "ClearLanding") _flightSimulation.ApproveLanding(commandFlight);
                else if (commandType == "ReturnBase") _flightSimulation.ReturnToOrigin(commandFlight);
                else if (commandType == "Divert") _flightSimulation.DivertToNearest(commandFlight);
                
                return RedirectToAction("ATCTower"); // PRG pattern
            }

            // Create Aircraft Colleagues for active flights and register to Tower
            var aircraftsDict = new Dictionary<string, TMPP_Aeroport.Domain.Mediator.Aircraft>();
            foreach (var flight in flights)
            {
                aircraftsDict[flight.FlightNumber] = new TMPP_Aeroport.Domain.Mediator.CommercialFlight(tower, flight.FlightNumber);
            }

            // Radio comms
            if (!string.IsNullOrEmpty(senderFlightNumber) && aircraftsDict.ContainsKey(senderFlightNumber))
            {
                var sender = aircraftsDict[senderFlightNumber];
                
                if (actionType == "Landing")
                {
                    sender.RequestLanding();
                }
                else if (!string.IsNullOrEmpty(message))
                {
                    sender.Send(message);
                }
            }

            ViewBag.Flights = flights;
            ViewBag.TowerLogs = tower.ATCLogs;
            ViewBag.AircraftsDict = aircraftsDict;
            ViewBag.PendingRequests = _flightSimulation.GetPendingRequests();

            return View();
        }

        // Observer Pattern: Global Alerts
        [HttpPost]
        public async Task<IActionResult> AlertsExecute(string flightNumber, string newStatus)
        {
            var flight = await _dbContext.Flights.FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            string oldStatus = "Unknown";
            string origin = "N/A";
            string destination = "N/A";

            if (flight != null)
            {
                oldStatus = flight.Status;
                origin = flight.Origin;
                destination = flight.Destination;

                flight.Status = newStatus;
                await _dbContext.SaveChangesAsync();
            }

            var subject = new TMPP_Aeroport.Domain.Observer.FlightStatusSubject(flightNumber);
            var logs = new List<string>();

            subject.Attach(new TMPP_Aeroport.Domain.Observer.PassengerNotifier(logs));
            subject.Attach(new TMPP_Aeroport.Domain.Observer.DisplayBoardUpdater(logs));

            subject.Status = newStatus;

            await _hubContext.Clients.All.SendAsync("FlightStateChanged", new {
                FlightNumber = flightNumber,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Origin = origin,
                Destination = destination
            });

            ViewBag.Logs = logs;
            ViewBag.FlightNumber = flightNumber;
            ViewBag.NewStatus = newStatus;
            ViewBag.Flights = await _dbContext.Flights.ToListAsync();

            return View("Alerts");
        }

        [HttpGet]
        public async Task<IActionResult> Alerts()
        {
            ViewBag.Flights = await _dbContext.Flights.ToListAsync();
            return View();
        }

        // Command Pattern: Runway Lights
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Command.RunwayReceiver> _receivers = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.Command.AtcInvoker> _invokers = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _lightsOnStates = new();

        private string GetSessionKey() => (User.Identity?.IsAuthenticated == true ? User.Identity.Name : null) ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        public async Task<IActionResult> RunwayLights(string commandName)
        {
            var key = GetSessionKey();
            var receiver = _receivers.GetOrAdd(key, _ => new TMPP_Aeroport.Domain.Command.RunwayReceiver());
            var invoker = _invokers.GetOrAdd(key, _ => new TMPP_Aeroport.Domain.Command.AtcInvoker());
            bool lightsOn = _lightsOnStates.GetOrAdd(key, false);

            if (!string.IsNullOrEmpty(commandName))
            {
                if (commandName == "ToggleLights")
                {
                    invoker.ExecuteCommand(new TMPP_Aeroport.Domain.Command.ToggleLightsCommand(receiver, lightsOn));
                    lightsOn = !lightsOn;
                    _lightsOnStates[key] = lightsOn;
                    await _hubContext.Clients.All.SendAsync("RunwayLights", lightsOn);
                }
                else if (commandName == "PrepareRunway")
                {
                    invoker.ExecuteCommand(new TMPP_Aeroport.Domain.Command.PrepareRunwayCommand(receiver));
                    lightsOn = true;
                    _lightsOnStates[key] = lightsOn;
                    await _hubContext.Clients.All.SendAsync("RunwayLights", lightsOn);
                }
                else if (commandName == "Undo")
                {
                    invoker.UndoLastCommand();
                }
            }

            ViewBag.Logs = receiver.Logs;
            return View();
        }
    }
}
