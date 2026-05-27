using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using System.Linq;
using System.Threading.Tasks;

namespace TMPP_Aeroport.Controllers
{
    public class AirportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TMPP_Aeroport.Domain.Adapter.IAirportWeatherService _weatherService;

        public AirportController(ApplicationDbContext dbContext, TMPP_Aeroport.Domain.Adapter.IAirportWeatherService weatherService)
        {
            _dbContext = dbContext;
            _weatherService = weatherService;
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

                var baggageTag = factory.CreateBaggageTag();
                baggageTag.Code = flightNumber + "-" + passengerName.GetHashCode().ToString("X");
                
                ViewBag.BoardingPass = boardingPass;
                ViewBag.BaggageTag = baggageTag;
            }

            return View();
        }

        // Builder Pattern: Flight Itinerary Planner
        public IActionResult BuilderDemo(string packageType = "economy", string actionType = "")
        {
            TMPP_Aeroport.Domain.Builder.IItineraryBuilder builder;
            if (packageType == "business") builder = new TMPP_Aeroport.Domain.Builder.BusinessItineraryBuilder();
            else builder = new TMPP_Aeroport.Domain.Builder.EconomyItineraryBuilder();

            var director = new TMPP_Aeroport.Domain.Builder.ItineraryDirector(builder);

            if (actionType == "no_baggage")
            {
                director.ConstructNoBaggageItinerary("Popescu Ion");
            }
            else
            {
                director.ConstructFullItinerary("Popescu Ion");
            }

            var itinerary = builder.GetResult();
            ViewBag.PackageType = packageType;
            ViewBag.Destination = "Paris (CDG)";
            return View(itinerary);
        }

        [HttpPost]
        public IActionResult BookItinerary(string packageType)
        {
            TempData["SuccessMessage"] = $"Your {packageType.ToUpper()} itinerary was booked and saved to your tickets!";
            return RedirectToAction("BuilderDemo", new { packageType = packageType });
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
