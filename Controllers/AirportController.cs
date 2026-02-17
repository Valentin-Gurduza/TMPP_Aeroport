using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using TMPP_Aeroport.Domain.Interfaces;

namespace TMPP_Aeroport.Controllers
{
    public class AirportController : Controller
    {
        private readonly IFlightService _flightService;
        private readonly IAircraftService _aircraftService;

        // DIP: Controller-ul depinde de abstracții (Interfețe), nu de clase concrete.
        public AirportController(IFlightService flightService, IAircraftService aircraftService)
        {
            _flightService = flightService;
            _aircraftService = aircraftService;
        }

        public IActionResult Index()
        {
            // Using ExpandoObject to pass multiple models to the view for simplicity in Lab 1
            dynamic model = new ExpandoObject();
            model.Flights = _flightService.GetAllFlights();
            model.Aircrafts = _aircraftService.GetAllAircraft();

            return View(model);
        }
    }
}
