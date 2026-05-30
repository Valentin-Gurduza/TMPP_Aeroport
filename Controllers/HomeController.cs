using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    public class HomeController : Controller
    {
        private readonly TMPP_Aeroport.Data.ApplicationDbContext _context;

        public HomeController(TMPP_Aeroport.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Dashboard Stats
            ViewBag.ActiveFlights = _context.Flights.Count(f => f.Status == "Scheduled" || f.Status == "Boarding" || f.Status == "ReadyForDeparture" || f.Status == "Airborne");
            ViewBag.TotalPassengers = _context.Tickets.Count(t => t.TicketState == "Issued" || t.TicketState == "CheckedIn" || t.TicketState == "Boarded");
            ViewBag.FleetSize = _context.Aircrafts.Count();
            ViewBag.RecentLogs = _context.AuditLogs.OrderByDescending(l => l.Timestamp).Take(5).ToList();
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Home/Error/{statusCode?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode.HasValue)
            {
                if (statusCode.Value == 404)
                {
                    return View("Error404");
                }
                if (statusCode.Value == 403)
                {
                    return View("~/Views/Shared/AccessDenied.cshtml");
                }
            }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
