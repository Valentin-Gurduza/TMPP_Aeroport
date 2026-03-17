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

        // Abstract Factory Pattern Usage
        // Actiunea CheckIn demonstreaza crearea unei familii de obiecte compatibile.
        [HttpGet]
        public IActionResult CheckIn(string ticketType)
        {
            TMPP_Aeroport.Domain.AbstractFactory.IFlightDocumentFactory factory;

            // Selectia fabricii se face la runtime (in functie de input-ul utilizatorului)
            switch (ticketType?.ToLower())
            {
                case "business":
                    factory = new TMPP_Aeroport.Domain.AbstractFactory.BusinessDocumentFactory();
                    break;
                case "economy":
                default:
                    factory = new TMPP_Aeroport.Domain.AbstractFactory.EconomyDocumentFactory();
                    break;
            }

            // Clientul (Controller-ul) nu stie ce clasa concreta de Bilet sau Eticheta va primi.
            // El stie doar ca Biletul si Eticheta vor fi compatibile intre ele (aceeasi familie).
            var boardingPass = factory.CreateBoardingPass();
            var baggageTag = factory.CreateBaggageTag();

            // Setam date demo
            boardingPass.PassengerName = "John Doe";
            boardingPass.FlightNumber = "RO301";
            baggageTag.Code = "BGG-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            dynamic model = new ExpandoObject();
            model.BoardingPass = boardingPass;
            model.BaggageTag = baggageTag;
            model.TicketType = ticketType ?? "Standard";

            return View(model);
        }

        // Builder Pattern Usage
        [HttpGet]
        public IActionResult BuilderDemo(string passengerName = "Maria Popescu", string type = "Business")
        {
            TMPP_Aeroport.Domain.Builder.IItineraryBuilder builder;
            
            if (type.ToLower() == "business")
                builder = new TMPP_Aeroport.Domain.Builder.BusinessItineraryBuilder();
            else
                builder = new TMPP_Aeroport.Domain.Builder.EconomyItineraryBuilder();

            var director = new TMPP_Aeroport.Domain.Builder.ItineraryDirector(builder);
            director.ConstructFullItinerary(passengerName);

            var itinerary = builder.GetResult();
            
            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log($"Itinerar (Builder) creat pentru {passengerName} de tip {type}");

            return View(itinerary);
        }

        // Prototype Pattern Usage
        public IActionResult PrototypeDemo(Guid? cloneFlightId)
        {
            if (cloneFlightId.HasValue)
            {
                // Clona pe baza prototipului selectat
                _flightService.CloneFlight(cloneFlightId.Value);
            }

            var flights = _flightService.GetAllFlights();
            return View(flights);
        }

        // Singleton Pattern Usage
        public IActionResult SingletonDemo()
        {
            var logs = TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.GetLogs();
            return View(logs);
        }
    }
}
