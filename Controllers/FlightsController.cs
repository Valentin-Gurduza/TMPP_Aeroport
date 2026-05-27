using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Hubs;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    [Authorize]
    public class FlightsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<FlightHub> _hubContext;

        public FlightsController(ApplicationDbContext context, IHubContext<FlightHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Flights
        // Accessible by all authenticated users (passengers see the board, admins manage it)
        public async Task<IActionResult> Index(int? pageNumber)
        {
            var flights = _context.Flights.Include(f => f.Aircraft).OrderByDescending(f => f.DepartureTime);
            int pageSize = 10;
            return View(await PaginatedList<Flight>.CreateAsync(flights.AsNoTracking(), pageNumber ?? 1, pageSize));
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

            if (!string.IsNullOrEmpty(status) && flight.Status != status)
            {
                flight.Status = status;
                await _context.SaveChangesAsync();
                
                // SignalR: Observer Pattern - Notificăm toți clienții conectați!
                await _hubContext.Clients.All.SendAsync("ReceiveFlightUpdate", flight.Id, flight.Status);
                
                TempData["SuccessMessage"] = $"Flight {flight.FlightNumber} status updated to {flight.Status}.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Flights/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var flight = await _context.Flights
                .Include(f => f.Aircraft)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flights/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight != null)
            {
                var flightNumber = flight.FlightNumber;
                _context.Flights.Remove(flight);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Flight {flightNumber} has been successfully deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
