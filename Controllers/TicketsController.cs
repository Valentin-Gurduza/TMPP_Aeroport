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

        // GET: /Tickets/Book
        public async Task<IActionResult> Book()
        {
            var flights = await _context.Flights
                .Include(f => f.Aircraft)
                .Where(f => f.Status == "Scheduled")
                .OrderBy(f => f.DepartureTime)
                .Take(20)
                .ToListAsync();

            return View(flights);
        }

        // GET: /Tickets/Buy/{id}
        public async Task<IActionResult> Buy(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Aircraft)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null || flight.Status != "Scheduled")
            {
                return NotFound("Flight is no longer available for booking.");
            }

            var takenSeats = await _context.Tickets
                .Where(t => t.FlightId == id && t.TicketState != "Cancelled")
                .Select(t => t.SeatNumber)
                .ToListAsync();

            ViewBag.Flight = flight;
            ViewBag.TakenSeats = takenSeats;
            return View();
        }

        // POST: /Tickets/BuyExecute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyExecute(int flightId, string seatNumbers, string fareClass)
        {
            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null || flight.Status != "Scheduled")
            {
                return BadRequest("Flight is no longer available.");
            }
            
            if (string.IsNullOrEmpty(seatNumbers))
            {
                return BadRequest("No seats selected.");
            }

            var seats = seatNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            var takenSeats = await _context.Tickets
                .Where(t => t.FlightId == flightId && t.TicketState != "Cancelled")
                .Select(t => t.SeatNumber)
                .ToListAsync();

            if (seats.Any(s => takenSeats.Contains(s)))
            {
                return BadRequest("One or more selected seats are already taken.");
            }

            decimal basePrice = fareClass == "Business" ? 350m : 120m;
            var userId = _userManager.GetUserId(User);
            int previousTickets = await _context.Tickets.CountAsync(t => t.UserId == userId);
            int frequentFlyerPoints = previousTickets * 100;

            TMPP_Aeroport.Domain.Strategy.ITicketPricingStrategy strategy;
            string strategyApplied = "Regular";
            
            bool isLastMinute = (flight.DepartureTime - DateTime.Now).TotalHours < 48;
            bool isVip = User.IsInRole("Admin") || User.IsInRole("ATC_Manager") || User.IsInRole("Ground_Staff");
            
            if (isLastMinute && isVip)
            {
                strategy = new TMPP_Aeroport.Domain.Strategy.VipLastMinutePricingStrategy();
                strategyApplied = "VIP Last Minute (-8% net)";
            }
            else if (isLastMinute)
            {
                strategy = new TMPP_Aeroport.Domain.Strategy.LastMinutePricingStrategy();
                strategyApplied = "Last Minute (+50%)";
            }
            else if (isVip)
            {
                strategy = new TMPP_Aeroport.Domain.Strategy.VipPricingStrategy();
                strategyApplied = "VIP Staff (-20%)";
            }
            else if (frequentFlyerPoints >= 100)
            {
                strategy = new TMPP_Aeroport.Domain.Strategy.FrequentFlyerPricingStrategy(frequentFlyerPoints);
                strategyApplied = $"Frequent Flyer (-{(frequentFlyerPoints/100)*5}%)";
            }
            else
            {
                strategy = new TMPP_Aeroport.Domain.Strategy.RegularPricingStrategy();
            }

            var context = new TMPP_Aeroport.Domain.Strategy.TicketContext(strategy);
            decimal price = (decimal)context.GetFinalPrice((double)basePrice);

            foreach (var seat in seats)
            {
                var ticket = new Ticket
                {
                    FlightId = flightId,
                    UserId = userId,
                    SeatNumber = seat,
                    Price = price,
                    TicketState = "WaitingForPayment",
                    FareClass = fareClass,
                    StrategyApplied = strategyApplied
                };
                _context.Tickets.Add(ticket);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully reserved {seats.Count} seats. Please complete the payment below.";
            return RedirectToAction(nameof(MyTickets));
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
            var ticket = await _context.Tickets.Include(t => t.Flight).Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket != null && ticket.TicketState == "WaitingForPayment")
            {
                var stateContext = new TMPP_Aeroport.Domain.State.TicketStateContext(ticket);
                stateContext.Pay(); // transitions to PaymentProcessing
                await _context.SaveChangesAsync();

                // Bug fix: replaced fire-and-forget Task.Run (race condition on restart)
                // Now we simulate the 3-second bank delay WITHIN the request scope
                // and complete processing synchronously before redirecting.
                await Task.Delay(2500); // Simulate bank processing (safe — request scope)

                // Re-fetch ticket with includes in same scope after delay
                var t = await _context.Tickets
                    .Include(x => x.Flight)
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (t != null && t.TicketState == "PaymentProcessing")
                {
                    var sc = new TMPP_Aeroport.Domain.State.TicketStateContext(t);
                    sc.Issue(); // transitions to Issued

                    // Abstract Factory: Generate Boarding Pass
                    TMPP_Aeroport.Domain.AbstractFactory.IFlightDocumentFactory factory;
                    if (t.FareClass == "Business") factory = new TMPP_Aeroport.Domain.AbstractFactory.BusinessDocumentFactory();
                    else factory = new TMPP_Aeroport.Domain.AbstractFactory.EconomyDocumentFactory();

                    var abstractPass = factory.CreateBoardingPass();
                    abstractPass.PassengerName = $"{t.User?.FirstName} {t.User?.LastName}";
                    abstractPass.FlightNumber = t.Flight?.FlightNumber ?? "UNK";
                    abstractPass.SeatNumber = t.SeatNumber;
                    abstractPass.Gate = t.Flight?.Gate ?? "B12";
                    abstractPass.Terminal = t.Flight?.Terminal ?? "T1";

                    // Persist boarding pass record to DB
                    var dbPass = new TMPP_Aeroport.Models.BoardingPass
                    {
                        TicketId = t.Id,
                        PassengerName = abstractPass.PassengerName,
                        FlightNumber = abstractPass.FlightNumber,
                        SeatNumber = t.SeatNumber,
                        Class = t.FareClass,
                        BarcodeData = $"BC-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                        IsDownloaded = false
                    };
                    _context.BoardingPasses.Add(dbPass);

                    // Builder: Generate Full Itinerary perks
                    TMPP_Aeroport.Domain.Builder.IItineraryBuilder builder = t.FareClass == "Business"
                        ? new TMPP_Aeroport.Domain.Builder.BusinessItineraryBuilder()
                        : new TMPP_Aeroport.Domain.Builder.EconomyItineraryBuilder();

                    var director = new TMPP_Aeroport.Domain.Builder.ItineraryDirector(builder);
                    director.ConstructFullItinerary(abstractPass.PassengerName);
                    var itinerary = builder.GetResult();
                    t.StrategyApplied += $" | Perks: {string.Join(",", itinerary.Meals)}";

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(MyTickets));
        }

        // POST: /Tickets/Cancel
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.Include(t => t.Flight).FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket == null)
                return Json(new { success = false, message = "Ticket not found." });

            // Bug fix: explicit state guard — Boarded and Refunded cannot be cancelled
            if (ticket.TicketState == "Boarded")
                return Json(new { success = false, message = "Cannot cancel a ticket after the passenger has boarded the aircraft." });

            if (ticket.TicketState == "Cancelled" || ticket.TicketState == "Refunded")
                return Json(new { success = false, message = "Ticket is already cancelled or refunded." });

            var stateContext = new TMPP_Aeroport.Domain.State.TicketStateContext(ticket);
            stateContext.Cancel();

            await _context.SaveChangesAsync();
            return Json(new { success = true, newState = "Cancelled", message = $"Ticket for flight {ticket.Flight?.FlightNumber} has been cancelled." });
        }

        // POST: /Tickets/Refund
        [HttpPost]
        public async Task<IActionResult> Refund(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket != null && ticket.TicketState == "Cancelled")
            {
                var stateContext = new TMPP_Aeroport.Domain.State.TicketStateContext(ticket);
                stateContext.Refund();
                
                await _context.SaveChangesAsync();
                return Json(new { success = true, newState = "Refunded", message = $"Ticket for flight {ticket.Flight?.FlightNumber} has been refunded to your account." });
            }

            return Json(new { success = false, message = "Could not refund ticket." });
        }

        // POST: /Tickets/CheckIn
        [HttpPost]
        public async Task<IActionResult> CheckIn(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket != null && ticket.TicketState == "Issued")
            {
                var stateContext = new TMPP_Aeroport.Domain.State.TicketStateContext(ticket);
                stateContext.CheckIn();
                ticket.CheckInAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return Json(new { success = true, newState = "CheckedIn", message = "You have successfully checked in for your flight." });
            }

            return Json(new { success = false, message = "Could not check in." });
        }

        // POST: /Tickets/Board
        [HttpPost]
        public async Task<IActionResult> Board(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket != null && ticket.TicketState == "CheckedIn")
            {
                var stateContext = new TMPP_Aeroport.Domain.State.TicketStateContext(ticket);
                stateContext.Board();
                ticket.BoardedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return Json(new { success = true, newState = "Boarded", message = "You have boarded the flight." });
            }

            return Json(new { success = false, message = "Could not board flight." });
        }

        // --- Design Patterns Moved from AirportController ---

        // 1. Strategy Pattern Usage
        public async Task<IActionResult> StrategyDemo(int? flightId, string strategyType = "regular")
        {
            var flights = await _context.Flights.Include(f => f.Aircraft).Where(f => f.Status == "Scheduled").Take(10).ToListAsync();
            ViewBag.Flights = flights;

            var userId = _userManager.GetUserId(User);
            int previousTickets = await _context.Tickets.CountAsync(t => t.UserId == userId);
            int frequentFlyerPoints = previousTickets * 100;

            double basePrice = 150; // Default fallback
            Models.Flight? selectedFlight = null;

            if (flightId.HasValue)
            {
                selectedFlight = flights.FirstOrDefault(f => f.Id == flightId.Value);
                if (selectedFlight != null)
                {
                    // Calculate base price dynamically based on duration/distance (mock)
                    basePrice = selectedFlight.Aircraft?.Type == "Wide-body" ? 250 : 120;
                }
            }

            TMPP_Aeroport.Domain.Strategy.ITicketPricingStrategy strategy;

            switch (strategyType.ToLower())
            {
                case "vip":
                    strategy = new TMPP_Aeroport.Domain.Strategy.VipPricingStrategy();
                    break;
                case "lastminute":
                    strategy = new TMPP_Aeroport.Domain.Strategy.LastMinutePricingStrategy();
                    break;
                case "frequentflyer":
                    strategy = new TMPP_Aeroport.Domain.Strategy.FrequentFlyerPricingStrategy(frequentFlyerPoints);
                    break;
                case "regular":
                default:
                    strategy = new TMPP_Aeroport.Domain.Strategy.RegularPricingStrategy();
                    break;
            }

            var context = new TMPP_Aeroport.Domain.Strategy.TicketContext(strategy);
            double finalPrice = context.GetFinalPrice(basePrice);

            ViewBag.SelectedFlight = selectedFlight;
            ViewBag.BasePrice = basePrice;
            ViewBag.FinalPrice = finalPrice;
            ViewBag.SelectedStrategy = strategyType;
            ViewBag.FrequentFlyerPoints = frequentFlyerPoints;

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
