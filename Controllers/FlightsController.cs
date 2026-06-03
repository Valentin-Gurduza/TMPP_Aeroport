using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Hubs;
using TMPP_Aeroport.Models;
using TMPP_Aeroport.Services;

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
            // Use the simulation's VirtualTime for FIDS visibility window.
            // Flights appear on FIDS 3 simulated hours before departure and
            // remain visible until they reach "Completed" status.
            var now = FlightSimulationService.VirtualTime;
            var fidsWindowStart = now.AddHours(-1);   // keep recently-landed flights visible up to 1h after landing
            var fidsWindowEnd   = now.AddHours(3);    // show flights departing within 3 virtual hours

            // Always show active flights regardless of time (Boarding, Airborne, Landed, Holding…)
            var activeStatuses = new[] { "Boarding", "Boarding Complete", "Airborne",
                                         "Cleared for Takeoff", "Awaiting Takeoff Clearance",
                                         "Awaiting Takeoff - Weather Hold",
                                         "Holding Pattern (Awaiting Landing)",
                                         "Holding Pattern (Weather - Storm)",
                                         "Cleared for Landing", "Landed" };

            var flights = _context.Flights
                .Include(f => f.Aircraft)
                .Where(f => f.Status != "Completed" &&
                    (
                        activeStatuses.Contains(f.Status) ||
                        (f.DepartureTime >= fidsWindowStart && f.DepartureTime <= fidsWindowEnd)
                    ))
                .OrderBy(f => f.DepartureTime);

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
