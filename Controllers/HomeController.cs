using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
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
