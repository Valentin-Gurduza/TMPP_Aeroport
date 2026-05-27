using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin,ATC_Manager")]
    public class FleetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FleetController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Fleet
        public async Task<IActionResult> Index(int? pageNumber)
        {
            var aircrafts = _context.Aircrafts.OrderBy(a => a.RegistrationCode);
            int pageSize = 10;
            return View(await PaginatedList<Aircraft>.CreateAsync(aircrafts.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Fleet/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Fleet/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("RegistrationCode,Model,Capacity,Airline")] Aircraft aircraft)
        {
            if (ModelState.IsValid)
            {
                _context.Add(aircraft);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Aircraft {aircraft.RegistrationCode} added to fleet.";
                return RedirectToAction(nameof(Index));
            }
            return View(aircraft);
        }

        // GET: Fleet/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var aircraft = await _context.Aircrafts.FindAsync(id);
            if (aircraft == null) return NotFound();

            return View(aircraft);
        }

        // POST: Fleet/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RegistrationCode,Model,Capacity,Airline")] Aircraft aircraft)
        {
            if (id != aircraft.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(aircraft);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Aircraft {aircraft.RegistrationCode} updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AircraftExists(aircraft.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(aircraft);
        }

        // POST: Fleet/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aircraft = await _context.Aircrafts.FindAsync(id);
            if (aircraft != null)
            {
                var code = aircraft.RegistrationCode;
                _context.Aircrafts.Remove(aircraft);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Aircraft {code} removed from fleet.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AircraftExists(int id)
        {
            return _context.Aircrafts.Any(e => e.Id == id);
        }
    }
}
