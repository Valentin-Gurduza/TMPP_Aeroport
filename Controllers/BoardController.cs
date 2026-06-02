using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Domain.Bridge;
using TMPP_Aeroport.Domain.Iterator;
using TMPP_Aeroport.Services;

namespace TMPP_Aeroport.Controllers
{
    public class BoardController : Controller
    {
        private readonly FlightSimulationService _simulationService;

        public BoardController(FlightSimulationService simulationService)
        {
            _simulationService = simulationService;
        }

        // FIDS Public Page
        [HttpGet]
        public IActionResult Index(string terminal = "All")
        {
            var activeFlights = _simulationService.GetActiveFlights().OrderBy(f => f.DepartureTime).ToList();

            // Setup Iterator Pattern for filtering
            var collection = new FlightScheduleCollection();
            foreach (var f in activeFlights)
            {
                // Extract terminal from gate if possible, otherwise default to Terminal A
                string term = !string.IsNullOrEmpty(f.AssignedGate) && f.AssignedGate.Length > 0 
                    ? $"Terminal {f.AssignedGate[0]}" 
                    : "Terminal A";
                
                collection.AddFlight(new FlightScheduleItem 
                { 
                    FlightNumber = f.FlightNumber, 
                    Terminal = term, 
                    Status = f.Status 
                });
            }

            IFlightIterator iterator;
            if (terminal != "All")
                iterator = collection.CreateTerminalIterator(terminal);
            else
                iterator = collection.CreateIterator();

            var filteredFlightNumbers = new List<string>();
            while (iterator.HasNext())
            {
                var item = iterator.Next();
                if (item != null) filteredFlightNumbers.Add(item.FlightNumber);
            }

            // Map SimulatedFlight to Models.Flight for the renderer
            var finalFlights = new List<TMPP_Aeroport.Models.Flight>();
            foreach(var f in activeFlights.Where(f => filteredFlightNumbers.Contains(f.FlightNumber)))
            {
                string term = !string.IsNullOrEmpty(f.AssignedGate) && f.AssignedGate.Length > 0 ? $"Terminal {f.AssignedGate[0]}" : "Terminal A";
                finalFlights.Add(new TMPP_Aeroport.Models.Flight
                {
                    FlightNumber = f.FlightNumber,
                    Origin = f.OriginName,
                    Destination = f.DestName,
                    DepartureTime = f.DepartureTime,
                    Status = f.Status,
                    Terminal = term,
                    Gate = f.AssignedGate
                });
            }

            // Setup Bridge Pattern for rendering
            IDisplayRenderer displayRenderer = new WebRenderer();

            FlightBoard board = new DeparturesBoard(displayRenderer);
            string renderedHtml = board.ShowBoard(finalFlights);

            ViewBag.RenderedHtml = renderedHtml;
            ViewBag.Terminal = terminal;

            return View();
        }
    }
}
