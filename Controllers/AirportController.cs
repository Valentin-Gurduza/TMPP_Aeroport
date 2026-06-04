using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using TMPP_Aeroport.Models;
using TMPP_Aeroport.Domain.Singleton;
using TMPP_Aeroport.Domain.Composite;
using TMPP_Aeroport.Domain.TemplateMethod;
using TMPP_Aeroport.Domain.Proxy;

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
        [Authorize(Roles = "Admin,ATC_Manager")]
        public IActionResult GetLogs()
        {
            var logs = TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.GetLogs();
            // return the last 10 logs
            var recentLogs = logs.Skip(System.Math.Max(0, logs.Count - 10)).ToList();
            return Json(recentLogs);
        }

    }
}
