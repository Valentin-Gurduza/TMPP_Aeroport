using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using TMPP_Aeroport.Domain.Interfaces;
using TMPP_Aeroport.Domain.Composite;

namespace TMPP_Aeroport.Controllers
{
    public class AirportController : Controller
    {
        private readonly IFlightService _flightService;
        private readonly IAircraftService _aircraftService;
        private readonly TMPP_Aeroport.Domain.Adapter.IAirportWeatherService _weatherService;

        // DIP: Controller-ul depinde de abstracții (Interfețe), nu de clase concrete.
        public AirportController(IFlightService flightService, IAircraftService aircraftService, TMPP_Aeroport.Domain.Adapter.IAirportWeatherService weatherService)
        {
            _flightService = flightService;
            _aircraftService = aircraftService;
            _weatherService = weatherService;
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

        // Adapter Pattern Usage
        public IActionResult AdapterDemo(string city = "BBU") // Bucharest by default
        {
            // Controller-ul MVC solicită temperatura în Celsius (curat, via Interfață standard)
            // Tot mecanismul de traducere din sistemul Legacy este ascuns de Adapter.
            double currentTemp = _weatherService.GetTemperatureCelsius(city);

            ViewBag.City = city;
            ViewBag.Temperature = currentTemp;

            return View();
        }

        // Composite Pattern Usage
        public IActionResult CompositeDemo()
        {
            // 1. Creăm Noduri Frunze simple
            ILuggageItem pasager1Valiza1 = new Suitcase("Valiză Roșie", 23.5);
            ILuggageItem pasager1Rucsac = new Backpack("Rucsac Laptop", 5.2);
            ILuggageItem pasager2Valiza = new Suitcase("Valiză Neagră", 18.0);

            // 2. Creăm un Container mediu (Familia Popescu) care combină valizele lor
            LuggageContainer popescuFamilyContainer = new LuggageContainer("POP-01");
            popescuFamilyContainer.Add(pasager1Valiza1);
            popescuFamilyContainer.Add(pasager1Rucsac);
            popescuFamilyContainer.Add(pasager2Valiza);

            // 3. Alt pasager separat
            ILuggageItem pasager3Valiza = new Suitcase("Geantă Golf", 12.0);

            // 4. Creăm Containerul Uriaș (Cala Avionului) care deține tot
            LuggageContainer mainCargo = new LuggageContainer("CARGO-MAIN-737");
            mainCargo.Add(popescuFamilyContainer); // Adăugăm un nod Compus înăuntrul altuia
            mainCargo.Add(pasager3Valiza);         // Adăugăm și o Frunză direct

            ViewBag.MainCargo = mainCargo;
            
            // Controllerul cere 'GetWeight()' o singură dată. Nu face bucle ForEach!
            ViewBag.TotalWeight = mainCargo.GetWeight();
            ViewBag.StringTree = mainCargo.Display();

            return View();
        }

        // Facade Pattern Usage
        [HttpPost]
        public IActionResult FacadeDemoExecute(string flightNumber, string runway)
        {
            // Controller-ul MVC apelează O SINGURĂ LINIUȚĂ DE COD pentru a decola!
            // Toată birocrația ATC/Crew/Baggage este ascunsă de obiectul Facade.
            
            var facade = new TMPP_Aeroport.Domain.Facade.FlightDepartureFacade();
            var flightLog = facade.AuthoriseDeparture(flightNumber, runway);

            ViewBag.FlightLog = flightLog;
            ViewBag.FlightNumber = flightNumber;
            return View("FacadeDemo");
        }

        [HttpGet]
        public IActionResult FacadeDemo()
        {
            return View();
        }
    }
}
