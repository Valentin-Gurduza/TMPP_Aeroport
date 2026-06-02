using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMPP_Aeroport.Data;
using System.Linq;

using Microsoft.AspNetCore.SignalR;
using TMPP_Aeroport.Hubs;
using TMPP_Aeroport.Domain.Observer;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin,Ground_Staff")]
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<FlightHub> _hubContext;

        public SecurityController(ApplicationDbContext context, IHubContext<FlightHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Chain of Responsibility Pattern: Security Checkpoint
        // UI Page
        public IActionResult Checkpoint()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult DebugBags()
        {
            var stats = _context.BaggageItems
                .GroupBy(b => new { b.SecurityStatus, b.BaggageStage })
                .Select(g => new { g.Key.SecurityStatus, g.Key.BaggageStage, Count = g.Count() })
                .ToList();
            var flights = _context.Flights.Select(f => new { f.FlightNumber, f.DepartureTime, f.Status }).ToList();
            return Json(new { bags = stats, flights = flights, virtualTime = TMPP_Aeroport.Services.FlightSimulationService.VirtualTime });
        }

        // AJAX API: Get Pending Baggages grouped by flight
        [HttpGet]
        public IActionResult GetPendingBaggages()
        {
            var pending = _context.BaggageItems
                .Where(b => b.SecurityStatus == "Pending" || b.SecurityStatus == "Flagged")
                .Select(b => new {
                    b.Id,
                    b.TagCode,
                    b.Weight,
                    b.FlightId,
                    b.SecurityStatus
                })
                .ToList();

            return Json(pending);
        }

        [HttpPost]
        public async Task<IActionResult> ScanBatch([FromBody] List<int> baggageIds)
        {
            var results = new List<object>();

            foreach (var id in baggageIds)
            {
                var dbBaggage = _context.BaggageItems.FirstOrDefault(b => b.Id == id);
                if (dbBaggage != null && dbBaggage.SecurityStatus == "Pending")
                {
                    await _hubContext.Clients.All.SendAsync("ScannerStatus", new { id = dbBaggage.Id, tag = dbBaggage.TagCode, stage = "Weight", status = "Scanning" });
                    await Task.Delay(800); // Simulate weight check

                    bool hasSuspicious = Random.Shared.Next(0, 100) < 15; // 15% chance
                    bool hasExplosive = Random.Shared.Next(0, 100) < 5;   // 5% chance

                    var baggage = new TMPP_Aeroport.Domain.ChainOfResponsibility.Baggage 
                    { 
                        Owner = "Tag: " + dbBaggage.TagCode, 
                        Weight = dbBaggage.Weight, 
                        HasSuspiciousItems = hasSuspicious, 
                        HasExplosiveTraces = hasExplosive 
                    };

                    var weightHandler = new TMPP_Aeroport.Domain.ChainOfResponsibility.WeightCheckHandler();
                    var xrayHandler = new TMPP_Aeroport.Domain.ChainOfResponsibility.XRayScanHandler();
                    var explosiveHandler = new TMPP_Aeroport.Domain.ChainOfResponsibility.ExplosiveTraceHandler();

                    weightHandler.SetNext(xrayHandler).SetNext(explosiveHandler);
                    
                    bool isApproved = weightHandler.Handle(baggage);

                    // Re-fetch to avoid context tracking issues across awaits if needed, though scoped should be fine here
                    dbBaggage.SecurityStatus = isApproved ? "Cleared" : "Flagged";
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("ScannerStatus", new { 
                        id = dbBaggage.Id, 
                        tag = dbBaggage.TagCode, 
                        stage = "Complete", 
                        status = isApproved ? "Cleared" : "Flagged",
                        logs = baggage.CheckLogs
                    });

                    results.Add(new {
                        id = dbBaggage.Id,
                        tag = dbBaggage.TagCode,
                        isApproved = isApproved,
                        logs = baggage.CheckLogs
                    });
                    
                    await Task.Delay(1000); // Delay before next bag
                }
            }
            return Json(results);
        }

        // AJAX API: Manual Inspection
        [HttpPost]
        public async Task<IActionResult> ManualInspect(int baggageId, bool clearBaggage)
        {
            var dbBaggage = _context.BaggageItems.FirstOrDefault(b => b.Id == baggageId);
            if (dbBaggage != null && dbBaggage.SecurityStatus == "Flagged")
            {
                if (clearBaggage)
                {
                    var baggage = new TMPP_Aeroport.Domain.ChainOfResponsibility.Baggage { Owner = "Tag: " + dbBaggage.TagCode };
                    var manualHandler = new TMPP_Aeroport.Domain.ChainOfResponsibility.ManualInspectionHandler();
                    manualHandler.Handle(baggage);
                    
                    dbBaggage.SecurityStatus = "Cleared";
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, action = "Cleared", logs = baggage.CheckLogs });
                }
                else
                {
                    dbBaggage.SecurityStatus = "Confiscated";
                    
                    // Observer Pattern: Trigger Security Alert
                    var alertSubject = new SecurityAlertSubject("CHK-MAIN-1");
                    alertSubject.Attach(new SignalRSecurityNotifier(_hubContext));
                    alertSubject.Attach(new DatabaseSecurityLogger(_context));
                    
                    alertSubject.TriggerAlert("CRITICAL", $"Baggage {dbBaggage.TagCode} confiscated due to illegal items. Security dispatched.");

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, action = "Confiscated" });
                }
            }
            return Json(new { success = false });
        }
    }
}
