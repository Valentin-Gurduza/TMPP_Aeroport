using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceScopeFactory _scopeFactory;

        public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _userManager = userManager;
            _scopeFactory = scopeFactory;
        }

        // GET: /Tickets/MyTickets
        public async Task<IActionResult> MyTickets(int? pageNumber)
        {
            var userId = _userManager.GetUserId(User);
            var tickets = _context.Tickets
                .Include(t => t.Flight)
                .ThenInclude(f => f!.Aircraft)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Flight!.DepartureTime);

            int pageSize = 10;
            return View(await PaginatedList<Ticket>.CreateAsync(tickets.AsNoTracking(), pageNumber ?? 1, pageSize));
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
        public async Task<IActionResult> BuyExecute(int flightId, string seatNumber, string fareClass)
        {
            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null || flight.Status != "Scheduled")
            {
                return BadRequest("Flight is no longer available.");
            }

            bool seatTaken = await _context.Tickets.AnyAsync(t => t.FlightId == flightId && t.SeatNumber == seatNumber && t.TicketState != "Cancelled");
            if (seatTaken)
            {
                return BadRequest("The selected seat is already taken.");
            }

            decimal price = fareClass == "Business" ? 350m : 120m;

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
                ticket.TicketState = "Validating";
                await _context.SaveChangesAsync();

                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000); // Simulate processing time
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var t = await db.Tickets.FindAsync(id);
                    if (t != null && t.TicketState == "Validating")
                    {
                        t.TicketState = "Issued";
                        await db.SaveChangesAsync();
                    }
                });
            }

            return RedirectToAction(nameof(MyTickets));
        }

        // POST: /Tickets/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket != null && ticket.TicketState != "Cancelled")
            {
                ticket.TicketState = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket for flight {ticket.Flight?.FlightNumber} has been cancelled.";
            }

            return RedirectToAction(nameof(MyTickets));
        }

        // --- Design Patterns Moved from AirportController ---

        // 1. Strategy Pattern Usage
        public IActionResult StrategyDemo(double basePrice = 100, string strategyType = "regular")
        {
            TMPP_Aeroport.Domain.Strategy.ITicketPricingStrategy strategy;

            switch (strategyType.ToLower())
            {
                case "vip":
                    strategy = new TMPP_Aeroport.Domain.Strategy.VipPricingStrategy();
                    break;
                case "lastminute":
                    strategy = new TMPP_Aeroport.Domain.Strategy.LastMinutePricingStrategy();
                    break;
                case "regular":
                default:
                    strategy = new TMPP_Aeroport.Domain.Strategy.RegularPricingStrategy();
                    break;
            }

            var context = new TMPP_Aeroport.Domain.Strategy.TicketContext(strategy);
            double finalPrice = context.GetFinalPrice(basePrice);

            ViewBag.BasePrice = basePrice;
            ViewBag.FinalPrice = finalPrice;
            ViewBag.SelectedStrategy = strategyType;

            return View();
        }

        // 2. State Pattern Usage — Ticket machine (any logged in user)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TMPP_Aeroport.Domain.State.TicketMachine> _ticketMachines = new();

        private TMPP_Aeroport.Domain.State.TicketMachine GetTicketMachine()
        {
            var key = (User.Identity?.IsAuthenticated == true ? User.Identity.Name : null) ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return _ticketMachines.GetOrAdd(key, _ => new TMPP_Aeroport.Domain.State.TicketMachine());
        }

        public IActionResult StateDemo(string actionType, int amount = 0)
        {
            var ticketMachine = GetTicketMachine();

            if (actionType == "Insert")
            {
                ticketMachine.InsertMoney(amount);
            }
            else if (actionType == "Request")
            {
                ticketMachine.RequestTicket();
            }
            else if (actionType == "Dispense")
            {
                int initialLogCount = ticketMachine.Logs.Count;
                ticketMachine.Dispense();
                
                // If a new log was added and it indicates success
                if (ticketMachine.Logs.Count > initialLogCount && ticketMachine.Logs.Last().Contains("Ticket printed and dispensed"))
                {
                    ViewBag.ShouldDownloadPDF = true;
                }
            }
            else if (actionType == "ResetLogs")
            {
                ticketMachine.Logs.Clear();
            }

            ViewBag.CurrentState = ticketMachine.State.GetType().Name;
            ViewBag.Balance = ticketMachine.Balance;
            ViewBag.Logs = new List<string>(ticketMachine.Logs);

            return View();
        }
    }
}
