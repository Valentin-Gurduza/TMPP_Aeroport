using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMPP_Aeroport.Data;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin,Ground_Staff")]
    public class SecurityController : Controller
    {
        // Chain of Responsibility Pattern: Security Checkpoint
        public IActionResult Checkpoint(double weight = 20, bool hasSuspicious = false, bool hasExplosive = false)
        {
            var baggage = new TMPP_Aeroport.Domain.ChainOfResponsibility.Baggage 
            { 
                Owner = "John Doe", 
                Weight = weight, 
                HasSuspiciousItems = hasSuspicious, 
                HasExplosiveTraces = hasExplosive 
            };

            var weightHandler = new TMPP_Aeroport.Domain.ChainOfResponsibility.WeightCheckHandler();
            var xrayHandler = new TMPP_Aeroport.Domain.ChainOfResponsibility.XRayScanHandler();
            var explosiveHandler = new TMPP_Aeroport.Domain.ChainOfResponsibility.ExplosiveTraceHandler();

            // Link the chain
            weightHandler.SetNext(xrayHandler).SetNext(explosiveHandler);

            bool isApproved = weightHandler.Handle(baggage);

            ViewBag.BaggageLogs = baggage.CheckLogs;
            ViewBag.IsApproved = isApproved;
            
            return View();
        }
    }
}
