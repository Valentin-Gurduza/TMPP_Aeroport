using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Passenger")]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Tickets/MyTickets
        public async Task<IActionResult> MyTickets()
        {
            var userId = _userManager.GetUserId(User);
            var tickets = await _context.Tickets
                .Include(t => t.Flight)
                .ThenInclude(f => f.Aircraft)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Flight.DepartureTime)
                .ToListAsync();

            return View(tickets);
        }

        // GET: /Tickets/Buy/{flightId}
        public async Task<IActionResult> Buy(int flightId)
        {
            var flight = await _context.Flights
                .Include(f => f.Aircraft)
                .FirstOrDefaultAsync(f => f.Id == flightId);

            if (flight == null || flight.Status != "Scheduled")
            {
                return NotFound("Flight is no longer available for booking.");
            }

            ViewBag.Flight = flight;
            return View();
        }

        // POST: /Tickets/BuyExecute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyExecute(int flightId, string seatNumber, decimal price)
        {
            var userId = _userManager.GetUserId(User);

            var ticket = new Ticket
            {
                FlightId = flightId,
                UserId = userId,
                SeatNumber = seatNumber,
                Price = price,
                TicketState = "WaitingForPayment" // Initial state
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Redirect to payment simulation
            return RedirectToAction(nameof(Pay), new { id = ticket.Id });
        }

        // GET: /Tickets/Pay/{id}
        public async Task<IActionResult> Pay(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets
                .Include(t => t.Flight)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket == null || ticket.TicketState != "WaitingForPayment")
            {
                return RedirectToAction(nameof(MyTickets));
            }

            return View(ticket);
        }

        // POST: /Tickets/PayExecute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayExecute(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket != null && ticket.TicketState == "WaitingForPayment")
            {
                // We simulate the State Pattern flow internally:
                // 1. Validating
                // 2. Issued
                ticket.TicketState = "Issued";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyTickets));
        }
    }
}
