using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    public class AirportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TMPP_Aeroport.Domain.Adapter.IAirportWeatherService _weatherService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AirportController(ApplicationDbContext dbContext, TMPP_Aeroport.Domain.Adapter.IAirportWeatherService weatherService, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _weatherService = weatherService;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var model = new TMPP_Aeroport.Models.DashboardViewModel();
            
            // Extragem statistici reale din Baza de Date
            model.TotalFlights = await _dbContext.Flights.CountAsync();
            model.ActiveFlights = await _dbContext.Flights.CountAsync(f => f.Status == TMPP_Aeroport.Models.FlightStatus.Airborne || f.Status == TMPP_Aeroport.Models.FlightStatus.Boarding);
            model.TotalAircrafts = await _dbContext.Aircrafts.CountAsync();
            
            var passengersRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Passenger");
            if (passengersRole != null)
            {
                model.TotalPassengers = await _dbContext.UserRoles.CountAsync(ur => ur.RoleId == passengersRole.Id);
            }
            else
            {
                model.TotalPassengers = 0;
            }

            model.TotalTickets = await _dbContext.Tickets.CountAsync();
            model.TotalRevenue = await _dbContext.Tickets.Where(t => t.TicketState == "Issued").SumAsync(t => (double?)t.Price) ?? 0.0;
            
            // Ultimele zboruri pentru activitate recentă
            model.RecentFlights = await _dbContext.Flights
                .Include(f => f.Aircraft)
                .OrderByDescending(f => f.DepartureTime)
                .Take(5)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetWeatherForCity(string city)
        {
            double currentTemp = _weatherService.GetTemperatureCelsius(city);
            return Json(new { city = city, temperatureCelsius = currentTemp });
        }

        [HttpGet]
        public IActionResult GetLogs()
        {
            var logs = TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.GetLogs();
            // return the last 10 logs
            var recentLogs = logs.Skip(System.Math.Max(0, logs.Count - 10)).ToList();
            return Json(recentLogs);
        }

        [Authorize]
        public IActionResult CheckIn(string passengerName, string flightNumber, string ticketType, int baggageKg = 23)
        {
            // Prefill: populate from authenticated user if values are not provided
            var userId = _userManager?.GetUserId(User);
            if (string.IsNullOrEmpty(passengerName) && userId != null)
            {
                var user = _dbContext.Users.Find(userId) as TMPP_Aeroport.Models.ApplicationUser;
                if (user != null)
                    passengerName = $"{user.FirstName} {user.LastName}".Trim();
            }
            if (string.IsNullOrEmpty(flightNumber) && userId != null)
            {
                // Pre-fill with the user's most recent issued ticket's flight
                var recentTicket = _dbContext.Tickets
                    .Include(t => t.Flight)
                    .Where(t => t.UserId == userId && t.TicketState == "Issued")
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefault();
                if (recentTicket?.Flight != null)
                    flightNumber = recentTicket.Flight.FlightNumber;
            }

            ViewBag.PassengerName = passengerName;
            ViewBag.FlightNumber = flightNumber;
            ViewBag.TicketType = ticketType ?? "Economy";
            ViewBag.BaggageKg = baggageKg;

            if (!string.IsNullOrEmpty(passengerName) && !string.IsNullOrEmpty(flightNumber))
            {
                TMPP_Aeroport.Domain.AbstractFactory.IFlightDocumentFactory factory;
                if (ticketType == "Business")
                    factory = new TMPP_Aeroport.Domain.AbstractFactory.BusinessDocumentFactory();
                else
                    factory = new TMPP_Aeroport.Domain.AbstractFactory.EconomyDocumentFactory();

                var boardingPass = factory.CreateBoardingPass();
                boardingPass.PassengerName = passengerName;
                boardingPass.FlightNumber = flightNumber;

                // Populate Gate, Terminal, SeatNumber from real DB data
                var flight = _dbContext.Flights.FirstOrDefault(f => f.FlightNumber == flightNumber);
                if (flight != null)
                {
                    boardingPass.Gate = string.IsNullOrEmpty(flight.Gate) ? "B12" : flight.Gate;
                    boardingPass.Terminal = string.IsNullOrEmpty(flight.Terminal) ? "T1" : flight.Terminal;
                }
                else
                {
                    boardingPass.Gate = "B12";
                    boardingPass.Terminal = "T1";
                }

                // Try to get real seat from ticket
                if (userId != null)
                {
                    var ticket = _dbContext.Tickets
                        .Include(t => t.Flight)
                        .FirstOrDefault(t =>
                            t.UserId == userId &&
                            t.Flight != null && t.Flight.FlightNumber == flightNumber &&
                            t.TicketState == "Issued");
                    boardingPass.SeatNumber = ticket?.SeatNumber ?? $"{(ticketType == "Business" ? "B" : "E")}{Random.Shared.Next(1, 35)}";
                }
                else
                {
                    boardingPass.SeatNumber = $"{(ticketType == "Business" ? "B" : "E")}{Random.Shared.Next(1, 35)}";
                }

                var baggageTag = factory.CreateBaggageTag();
                baggageTag.Code = flightNumber + "-" + passengerName.GetHashCode().ToString("X");
                
                ViewBag.BoardingPass = boardingPass;
                ViewBag.BaggageTag = baggageTag;
            }

            return View();
        }

        // Builder & Prototype Pattern: Flight Itinerary Planner
        [Authorize]
        public IActionResult BuilderDemo(string packageType = "economy", string actionType = "", string? passengerName = null, int? flightId = null)
        {
            ViewBag.Flights = _dbContext.Flights.Where(f => f.Status == "Scheduled" || f.Status == "Boarding").ToList();
            
            TMPP_Aeroport.Domain.Builder.IItineraryBuilder builder;
            if (packageType == "business") builder = new TMPP_Aeroport.Domain.Builder.BusinessItineraryBuilder();
            else builder = new TMPP_Aeroport.Domain.Builder.EconomyItineraryBuilder();

            var director = new TMPP_Aeroport.Domain.Builder.ItineraryDirector(builder);

            // Use real authenticated user's name if empty
            var userId = _userManager?.GetUserId(User);
            if (string.IsNullOrEmpty(passengerName) && userId != null)
            {
                var user = _dbContext.Users.Find(userId) as TMPP_Aeroport.Models.ApplicationUser;
                if (user != null) passengerName = $"{user.FirstName} {user.LastName}".Trim();
            }
            if (string.IsNullOrEmpty(passengerName)) passengerName = "Pasager Demo";

            if (actionType == "no_baggage")
            {
                director.ConstructNoBaggageItinerary(passengerName);
            }
            else
            {
                director.ConstructFullItinerary(passengerName);
            }

            var itinerary = builder.GetResult();
            var itineraries = new List<TMPP_Aeroport.Domain.Builder.FlightItinerary> { itinerary };

            // Prototype Pattern usage for Group Booking
            if (actionType == "group")
            {
                var names = new[] { "Popescu Maria", "Popescu Andrei", "Popescu Elena" };
                foreach (var name in names)
                {
                    // Clone the complex object
                    var clone = (TMPP_Aeroport.Domain.Builder.FlightItinerary)itinerary.Clone();
                    clone.PassengerName = name; // Only modify the name
                    itineraries.Add(clone);
                }
            }

            ViewBag.PackageType = packageType;
            ViewBag.ActionType = actionType;
            ViewBag.PassengerName = passengerName;
            ViewBag.SelectedFlightId = flightId ?? ViewBag.Flights?[0]?.Id;

            // Fetch destination from selected flight
            string destination = "Paris (CDG)";
            if (ViewBag.SelectedFlightId != null)
            {
                var flight = _dbContext.Flights.Find(ViewBag.SelectedFlightId);
                if (flight != null) destination = flight.Destination;
            }
            ViewBag.Destination = destination;
            
            return View(itineraries);
        }

        [HttpPost]
        public async Task<IActionResult> BookItinerary(string packageType, string actionType, string passengerName, int flightId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                var names = new List<string> { passengerName };
                if (actionType == "group")
                {
                    names.Clear();
                    names.AddRange(new[] { "Popescu Maria", "Popescu Andrei", "Popescu Elena" });
                }

                decimal price = packageType == "business" ? 450m : 150m;

                foreach (var name in names)
                {
                    var ticket = new Ticket
                    {
                        FlightId = flightId,
                        UserId = userId,
                        SeatNumber = (packageType == "business" ? "B" : "E") + Random.Shared.Next(1, 40),
                        Price = price,
                        TicketState = "Issued", // Issue directly from Itinerary Builder for demo purposes
                        FareClass = packageType == "business" ? "Business" : "Economy",
                        StrategyApplied = "Itinerary Package"
                    };
                    _dbContext.Tickets.Add(ticket);
                }
                
                await _dbContext.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Your {packageType.ToUpper()} itinerary was booked and saved to your tickets!";
            return RedirectToAction("BuilderDemo", new { packageType = packageType, actionType = actionType, passengerName = passengerName, flightId = flightId });
        }

        // Proxy Pattern Demo
        [Authorize]
        public IActionResult ProxyDemo()
        {
            ViewBag.Flights = _dbContext.Flights.Take(10).ToList();
            
            // By default use the user's roles for simulation
            var role = "Passenger";
            if (User.IsInRole("ATC_Manager")) role = "ATC_Manager";
            else if (User.IsInRole("Admin")) role = "Admin";
            
            ViewBag.RoleAttempted = role;
            
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult ProxyDemoExecute(string flightNumber, string actionType)
        {
            // Instead of reading the dropdown, we read the real user's role:
            var role = "Passenger";
            if (User.IsInRole("ATC_Manager")) role = "ATC_Manager";
            else if (User.IsInRole("Admin")) role = "Admin";

            TMPP_Aeroport.Domain.Proxy.IRunwayControl proxy = new TMPP_Aeroport.Domain.Proxy.RunwayControlProxy(role);
            string result = "";
            string runway = "08R"; // hardcoded for demo

            if (actionType == "clearance")
            {
                result = proxy.GrantClearance(flightNumber, runway);
            }
            else if (actionType == "lock")
            {
                result = proxy.LockRunway(runway);
            }

            ViewBag.Flights = _dbContext.Flights.Take(10).ToList();
            ViewBag.RoleAttempted = role;
            ViewBag.Result = result;

            return View("ProxyDemo");
        }
    }
}
