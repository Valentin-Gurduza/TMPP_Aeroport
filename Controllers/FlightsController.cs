using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    [Authorize]
    public class FlightsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Flights
        // Accessible by all authenticated users (passengers see the board, admins manage it)
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Flights.Include(f => f.Aircraft);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Flights/Create
        [Authorize(Roles = "Admin,ATC_Manager")]
        public IActionResult Create()
        {
            ViewData["AircraftId"] = new SelectList(_context.Aircrafts, "Id", "RegistrationCode");
            return View();
        }

        // POST: Flights/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ATC_Manager")]
        public async Task<IActionResult> Create([Bind("FlightNumber,Destination,DepartureTime,Status,AircraftId")] Flight flight)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flight);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AircraftId"] = new SelectList(_context.Aircrafts, "Id", "RegistrationCode", flight.AircraftId);
            return View(flight);
        }

        // GET: Flights/EditStatus/5
        [Authorize(Roles = "Admin,ATC_Manager")]
        public async Task<IActionResult> EditStatus(int? id)
        {
            if (id == null) return NotFound();

            var flight = await _context.Flights.Include(f => f.Aircraft).FirstOrDefaultAsync(m => m.Id == id);
            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flights/EditStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ATC_Manager")]
        public async Task<IActionResult> EditStatus(int id, string status)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null) return NotFound();

            if (!string.IsNullOrEmpty(status))
            {
                flight.Status = status;
                
                // TODO: Integrate SignalR notification in Phase 4
                // await _hubContext.Clients.All.SendAsync("ReceiveFlightUpdate", flight.FlightNumber, flight.Status);
                
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
