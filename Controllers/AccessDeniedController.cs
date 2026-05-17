using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMPP_Aeroport.Controllers
{
    public class AccessDeniedController : Controller
    {
        [HttpGet]
        [Route("Airport/AccessDenied")]
        public IActionResult Index()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }
    }
}
