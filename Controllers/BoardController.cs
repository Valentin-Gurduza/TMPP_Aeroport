using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Domain.Bridge;
using TMPP_Aeroport.Domain.Iterator;

namespace TMPP_Aeroport.Controllers
{
    public class BoardController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public BoardController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // FIDS Public Page
        [HttpGet]
        public async Task<IActionResult> Index(string terminal = "All", string renderer = "Web")
        {
            var dbFlights = await _dbContext.Flights.OrderBy(f => f.DepartureTime).ToListAsync();

            // Setup Iterator Pattern for filtering
            var collection = new FlightScheduleCollection();
            foreach (var f in dbFlights)
            {
                // Bug fix: use real Terminal from DB if available
                string term = !string.IsNullOrEmpty(f.Terminal) ? f.Terminal : "Terminal A";
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

            var finalFlights = dbFlights.Where(f => filteredFlightNumbers.Contains(f.FlightNumber)).ToList();

            // Setup Bridge Pattern for rendering
            IDisplayRenderer displayRenderer;
            if (renderer == "LED")
                displayRenderer = new LEDRenderer();
            else
                displayRenderer = new WebRenderer();

            FlightBoard board = new DeparturesBoard(displayRenderer);
            string renderedHtml = board.ShowBoard(finalFlights);

            ViewBag.RenderedHtml = renderedHtml;
            ViewBag.Terminal = terminal;
            ViewBag.Renderer = renderer;

            return View();
        }
    }
}
