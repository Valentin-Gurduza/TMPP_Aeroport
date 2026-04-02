using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using TMPP_Aeroport.Domain.Interfaces;
using TMPP_Aeroport.Domain.Composite;
using TMPP_Aeroport.Domain.Flyweight;
using TMPP_Aeroport.Domain.Decorator;
using TMPP_Aeroport.Domain.Bridge;
using TMPP_Aeroport.Domain.Proxy;

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

        // ==========================================
        // LAB 5: Flyweight, Decorator, Bridge, Proxy
        // ==========================================

        // 1. Flyweight Pattern Usage
        public IActionResult FlyweightDemo()
        {
            var factory = new AircraftModelFactory();
            var blips = new List<RadarBlip>();

            // Creăm 50.000 de puncte radar pe ecran
            for (int i = 0; i < 50000; i++)
            {
                // Modelele grele 'Boeing 737' și 'Airbus A320' sunt reciclate din Factory, 
                // scutind zeci de Gigabytes de RAM.
                string modelName = (i % 2 == 0) ? "Boeing 737" : "Airbus A320";
                
                blips.Add(new RadarBlip(
                    flightNum: $"FL-{i}", 
                    lat: 44.4 + (i * 0.0001), 
                    lon: 26.1 + (i * 0.0001), 
                    model: factory.GetAircraftModel(modelName)
                ));
            }

            ViewBag.TotalObjects = blips.Count;
            ViewBag.UniqueModelsInMemory = factory.GetCacheSize(); // Doar 2!

            return View();
        }

        // 2. Decorator Pattern Usage
        [HttpPost]
        public IActionResult DecoratorDemoExecute(string passengerName, bool sms, bool email)
        {
            // Baza standard
            IBoardingNotifier notifier = new WebAppNotifier();

            // Decorăm dinamic în funcție de preferințele utilizatorului
            if (sms)
                notifier = new SMSNotifierDecorator(notifier);
            if (email)
                notifier = new EmailNotifierDecorator(notifier);

            // Apelăm notificarea "înfășurată".
            var logs = notifier.SendNotification(passengerName, "Zborul tău a fost programat la Poarta 4.");

            ViewBag.Logs = logs;
            ViewBag.PassengerName = passengerName;

            return View("DecoratorDemo");
        }

        [HttpGet]
        public IActionResult DecoratorDemo()
        {
            return View();
        }

        // 3. Bridge Pattern Usage
        public IActionResult BridgeDemo(string hardware = "led")
        {
            // Listă falsă de zboruri pentru display
            var flights = new List<string> { "RO-302", "WZZ-15K", "LH-1652", "BA-092" };

            // 1) Alegem Implementarea Hardware (LED sau Web/SmartTV)
            IDisplayRenderer renderer = (hardware == "web") ? new WebRenderer() : new LEDRenderer();

            // 2) Alegem Abstractizarea de Business (Plecări sau Sosiri)
            FlightBoard departures = new DeparturesBoard(renderer);
            FlightBoard arrivals = new ArrivalsBoard(renderer);

            ViewBag.Hardware = hardware;
            ViewBag.DeparturesRender = departures.ShowBoard(flights);
            ViewBag.ArrivalsRender = arrivals.ShowBoard(flights);

            return View();
        }

        // 4. Proxy Pattern Usage
        [HttpPost]
        public IActionResult ProxyDemoExecute(string role, string actionType)
        {
            // Creăm interfața Proxy, nu instanțiem direct serviciul periculos.
            IRunwayControl runwayProxy = new RunwayControlProxy(role);
            string result = "";

            if (actionType == "clearance")
            {
                result = runwayProxy.GrantClearance("TAROM-102", "Pista Nord 01L");
            }
            else if (actionType == "lock")
            {
                result = runwayProxy.LockRunway("Pista Nord 01L");
            }

            ViewBag.RoleAttempted = role;
            ViewBag.Result = result;
            return View("ProxyDemo");
        }

        [HttpGet]
        public IActionResult ProxyDemo()
        {
            return View();
        }
    }
}
