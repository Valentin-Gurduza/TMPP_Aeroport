using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Domain.Interfaces;
using TMPP_Aeroport.Domain.Composite;
using TMPP_Aeroport.Domain.Flyweight;
using TMPP_Aeroport.Domain.Decorator;
using TMPP_Aeroport.Domain.Bridge;
using TMPP_Aeroport.Domain.Proxy;
using TMPP_Aeroport.Data;

namespace TMPP_Aeroport.Controllers
{
    public class AirportController : Controller
    {
        private readonly IFlightService _flightService;
        private readonly IAircraftService _aircraftService;
        private readonly TMPP_Aeroport.Domain.Adapter.IAirportWeatherService _weatherService;
        private readonly ApplicationDbContext _dbContext;

        // DIP: Controller-ul depinde de abstracții (Interfețe), nu de clase concrete.
        public AirportController(IFlightService flightService, IAircraftService aircraftService, TMPP_Aeroport.Domain.Adapter.IAirportWeatherService weatherService, ApplicationDbContext dbContext)
        {
            _flightService = flightService;
            _aircraftService = aircraftService;
            _weatherService = weatherService;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            dynamic model = new ExpandoObject();
            
            // Extragem statistici reale din Baza de Date
            model.TotalFlights = await _dbContext.Flights.CountAsync();
            model.ActiveFlights = await _dbContext.Flights.CountAsync(f => f.Status == "Airborne" || f.Status == "Boarding");
            model.TotalAircrafts = await _dbContext.Aircrafts.CountAsync();
            
            var passengersRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Passenger");
            if (passengersRole != null)
            {
                model.TotalPassengers = await _dbContext.UserRoles.CountAsync(ur => ur.RoleId == passengersRole.Id);
            }
            else
            {
                model.TotalPassengers = 0;
            }

            model.TotalTickets = await _dbContext.Tickets.CountAsync();
            model.TotalRevenue = await _dbContext.Tickets.Where(t => t.TicketState == "Issued").SumAsync(t => (double?)t.Price) ?? 0.0;
            
            // Ultimele zboruri pentru activitate recentă
            model.RecentFlights = await _dbContext.Flights
                .Include(f => f.Aircraft)
                .OrderByDescending(f => f.DepartureTime)
                .Take(5)
                .ToListAsync();

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

        // Facade Pattern Usage — ATC Tower (ATC_Manager or Admin only)
        [Authorize(Roles = "Admin,ATC_Manager")]
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

        [Authorize(Roles = "Admin,ATC_Manager")]
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

        // 4. Proxy Pattern Usage — Security Control (Admin or ATC_Manager)
        [Authorize(Roles = "Admin,ATC_Manager")]
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

        [Authorize(Roles = "Admin,ATC_Manager")]
        [HttpGet]
        public IActionResult ProxyDemo()
        {
            return View();
        }
        // ==========================================
        // LAB 6: Strategy, Observer, Command, Memento, Iterator
        // ==========================================

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

        // 2. Observer Pattern Usage
        [HttpPost]
        public IActionResult ObserverDemoExecute(string flightNumber, string newStatus)
        {
            // Setăm status inițial
            var subject = new TMPP_Aeroport.Domain.Observer.FlightStatusSubject(flightNumber);
            var logs = new List<string>();

            // Adăugăm observatorii (pot fi adăugați/scoși dinamic)
            subject.Attach(new TMPP_Aeroport.Domain.Observer.PassengerNotifier(logs));
            subject.Attach(new TMPP_Aeroport.Domain.Observer.DisplayBoardUpdater(logs));

            // Schimbarea stării va declanșa notificările automat
            subject.Status = newStatus;

            ViewBag.Logs = logs;
            ViewBag.FlightNumber = flightNumber;
            ViewBag.NewStatus = newStatus;

            return View("ObserverDemo");
        }

        [HttpGet]
        public IActionResult ObserverDemo()
        {
            return View();
        }

        // 3. Command Pattern Usage
        // Instanțe statice pentru demo, pentru a păstra starea între requesturi
        private static TMPP_Aeroport.Domain.Command.RunwayReceiver _receiver = new TMPP_Aeroport.Domain.Command.RunwayReceiver();
        private static TMPP_Aeroport.Domain.Command.AtcInvoker _invoker = new TMPP_Aeroport.Domain.Command.AtcInvoker();
        private static bool _lightsOn = false;

        public IActionResult CommandDemo(string commandName)
        {
            if (!string.IsNullOrEmpty(commandName))
            {
                if (commandName == "ToggleLights")
                {
                    _invoker.ExecuteCommand(new TMPP_Aeroport.Domain.Command.ToggleLightsCommand(_receiver, _lightsOn));
                    _lightsOn = !_lightsOn;
                }
                else if (commandName == "PrepareRunway")
                {
                    _invoker.ExecuteCommand(new TMPP_Aeroport.Domain.Command.PrepareRunwayCommand(_receiver));
                    _lightsOn = true;
                }
                else if (commandName == "Undo")
                {
                    _invoker.UndoLastCommand();
                }
            }

            ViewBag.Logs = _receiver.Logs;
            return View();
        }

        // 4. Memento Pattern Usage
        private static TMPP_Aeroport.Domain.Memento.FlightConfigurator _originator = new TMPP_Aeroport.Domain.Memento.FlightConfigurator() { Gate = "A1", DepartureTime = DateTime.Now.AddHours(2), AircraftModel = "Boeing 737" };
        private static TMPP_Aeroport.Domain.Memento.FlightConfigHistory _caretaker = new TMPP_Aeroport.Domain.Memento.FlightConfigHistory();

        public IActionResult MementoDemo(string actionType, string newGate, string newModel)
        {
            if (actionType == "Save")
            {
                _caretaker.Backup(_originator);
            }
            else if (actionType == "Update")
            {
                _originator.SetConfiguration(newGate ?? "A1", DateTime.Now.AddHours(3), newModel ?? "Airbus A320");
            }
            else if (actionType == "Undo")
            {
                _caretaker.Undo(_originator);
            }

            ViewBag.CurrentGate = _originator.Gate;
            ViewBag.CurrentModel = _originator.AircraftModel;
            ViewBag.Logs = _originator.ActionLogs;

            return View();
        }

        // 5. Iterator Pattern Usage
        public IActionResult IteratorDemo(string targetTerminal)
        {
            var collection = new TMPP_Aeroport.Domain.Iterator.FlightScheduleCollection();
            collection.AddFlight(new TMPP_Aeroport.Domain.Iterator.FlightScheduleItem { FlightNumber = "RO101", Terminal = "T1", Status = "On Time" });
            collection.AddFlight(new TMPP_Aeroport.Domain.Iterator.FlightScheduleItem { FlightNumber = "RO202", Terminal = "T2", Status = "Delayed" });
            collection.AddFlight(new TMPP_Aeroport.Domain.Iterator.FlightScheduleItem { FlightNumber = "RO303", Terminal = "T1", Status = "Boarding" });
            collection.AddFlight(new TMPP_Aeroport.Domain.Iterator.FlightScheduleItem { FlightNumber = "RO404", Terminal = "T3", Status = "On Time" });

            TMPP_Aeroport.Domain.Iterator.IFlightIterator iterator;
            if (string.IsNullOrEmpty(targetTerminal) || targetTerminal == "All")
            {
                iterator = collection.CreateIterator();
            }
            else
            {
                iterator = collection.CreateTerminalIterator(targetTerminal);
            }

            var resultFlights = new List<TMPP_Aeroport.Domain.Iterator.FlightScheduleItem>();
            while (iterator.HasNext())
            {
                var nextItem = iterator.Next();
                if (nextItem != null)
                {
                    resultFlights.Add(nextItem);
                }
            }

            ViewBag.Flights = resultFlights;
            ViewBag.SelectedTerminal = targetTerminal ?? "All";

            return View();
        }
        // ==========================================
        // LAB 7: Chain of Responsibility, State, Mediator, Template Method, Visitor
        // ==========================================

        // 1. Chain of Responsibility Pattern Usage — Baggage (Ground_Staff or Admin)
        [Authorize(Roles = "Admin,Ground_Staff")]
        public IActionResult ChainDemo(double weight = 20, bool hasSuspicious = false, bool hasExplosive = false)
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

        // 2. State Pattern Usage — Ticket machine (any logged in user)
        private static TMPP_Aeroport.Domain.State.TicketMachine _ticketMachine = new TMPP_Aeroport.Domain.State.TicketMachine();

        [Authorize]
        public IActionResult StateDemo(string actionType, int amount = 0)
        {
            if (actionType == "Insert")
            {
                _ticketMachine.InsertMoney(amount);
            }
            else if (actionType == "Request")
            {
                _ticketMachine.RequestTicket();
            }
            else if (actionType == "Dispense")
            {
                _ticketMachine.Dispense();
            }
            else if (actionType == "ResetLogs")
            {
                _ticketMachine.Logs.Clear();
            }

            ViewBag.CurrentState = _ticketMachine.State.GetType().Name;
            ViewBag.Balance = _ticketMachine.Balance;
            ViewBag.Logs = _ticketMachine.Logs;

            return View();
        }

        // 3. Mediator Pattern Usage — ATC Tower (ATC_Manager or Admin)
        [Authorize(Roles = "Admin,ATC_Manager")]
        public IActionResult MediatorDemo(string actionType)
        {
            var tower = new TMPP_Aeroport.Domain.Mediator.ATCTower();
            var flight1 = new TMPP_Aeroport.Domain.Mediator.CommercialFlight(tower, "TAROM-101");
            var flight2 = new TMPP_Aeroport.Domain.Mediator.CommercialFlight(tower, "WIZZ-777");
            var heli1 = new TMPP_Aeroport.Domain.Mediator.Helicopter(tower, "HELI-MEDEVAC");

            if (actionType == "Broadcast")
            {
                flight1.Send("Encountering severe turbulence at FL350.");
            }
            else if (actionType == "Landing")
            {
                flight2.RequestLanding();
            }
            else
            {
                heli1.Send("Entering airspace.");
            }

            ViewBag.TowerLogs = tower.ATCLogs;
            ViewBag.Flight1Logs = flight1.AircraftLogs;
            ViewBag.Flight2Logs = flight2.AircraftLogs;
            ViewBag.HeliLogs = heli1.AircraftLogs;

            return View();
        }

        // 4. Template Method Pattern Usage
        public IActionResult TemplateMethodDemo(string flightType = "passenger")
        {
            TMPP_Aeroport.Domain.TemplateMethod.FlightPreflightRoutine routine;

            if (flightType == "cargo")
            {
                routine = new TMPP_Aeroport.Domain.TemplateMethod.CargoFlightRoutine();
            }
            else
            {
                routine = new TMPP_Aeroport.Domain.TemplateMethod.PassengerFlightRoutine();
            }

            routine.ExecuteRoutine();

            ViewBag.RoutineLogs = routine.RoutineLogs;
            ViewBag.FlightType = flightType;

            return View();
        }

        // 5. Visitor Pattern Usage
        public IActionResult VisitorDemo(string format = "json")
        {
            var elements = new List<TMPP_Aeroport.Domain.Visitor.IAirportElement>
            {
                new TMPP_Aeroport.Domain.Visitor.TerminalElement("Terminal 1", 15),
                new TMPP_Aeroport.Domain.Visitor.AircraftElement("Boeing 737 MAX", 180),
                new TMPP_Aeroport.Domain.Visitor.FlightElement("RO-302", "Frankfurt")
            };

            TMPP_Aeroport.Domain.Visitor.IVisitor visitor;
            
            if (format == "xml")
            {
                visitor = new TMPP_Aeroport.Domain.Visitor.XmlExportVisitor();
            }
            else
            {
                visitor = new TMPP_Aeroport.Domain.Visitor.JsonExportVisitor();
            }

            foreach (var element in elements)
            {
                element.Accept(visitor);
            }

            if (format == "xml")
            {
                ViewBag.ExportData = ((TMPP_Aeroport.Domain.Visitor.XmlExportVisitor)visitor).ExportedData;
            }
            else
            {
                ViewBag.ExportData = ((TMPP_Aeroport.Domain.Visitor.JsonExportVisitor)visitor).ExportedData;
            }

            ViewBag.Format = format;

            return View();
        }
    }
}
