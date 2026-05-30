using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Domain.TemplateMethod
{
    public abstract class FlightPreflightRoutine
    {
        protected ApplicationDbContext _context;
        protected Flight _flight;

        public List<string> RoutineLogs { get; } = new List<string>();
        public bool IsSuccessful { get; private set; } = true;

        public FlightPreflightRoutine(ApplicationDbContext context, Flight flight)
        {
            _context = context;
            _flight = flight;
        }

        public void ExecuteRoutine()
        {
            RoutineLogs.Add($"--- Starting Pre-flight Routine for {_flight.FlightNumber} ---");
            if (!CheckFuel()) IsSuccessful = false;
            if (!CheckSystems()) IsSuccessful = false;
            if (!LoadSpecificPayload()) IsSuccessful = false;
            if (!BriefCrew()) IsSuccessful = false;
            
            if (IsSuccessful)
                RoutineLogs.Add("--- Pre-flight Routine Complete [SUCCESS] ---");
            else
                RoutineLogs.Add("--- Pre-flight Routine Aborted [FAILED] ---");
        }

        protected virtual bool CheckFuel()
        {
            RoutineLogs.Add("✅ Checking fuel levels. Nominal.");
            return true;
        }

        protected virtual bool CheckSystems()
        {
            RoutineLogs.Add("✅ Checking avionics and hydraulics. Systems GO.");
            return true;
        }

        protected virtual bool BriefCrew()
        {
            RoutineLogs.Add("✅ Briefing crew on weather and flight plan.");
            return true;
        }

        protected abstract bool LoadSpecificPayload();
    }

    public class PassengerFlightRoutine : FlightPreflightRoutine
    {
        public PassengerFlightRoutine(ApplicationDbContext context, Flight flight) : base(context, flight) {}

        protected override bool LoadSpecificPayload()
        {
            var tickets = _context.Tickets.Where(t => t.FlightId == _flight.Id).ToList();
            var issuedTickets = tickets.Count(t => t.TicketState == "Issued" || t.TicketState == "CheckedIn" || t.TicketState == "Boarded");
            
            if (issuedTickets > _flight.MaxCapacity && _flight.MaxCapacity > 0)
            {
                RoutineLogs.Add($"❌ OVERBOOKED! {issuedTickets} passengers for {_flight.MaxCapacity} seats.");
                return false;
            }
            
            RoutineLogs.Add($"✅ 👥 Processing {issuedTickets} passengers.");
            RoutineLogs.Add("✅ 🍲 Catering supplies onboard.");
            return true;
        }
    }

    public class CargoFlightRoutine : FlightPreflightRoutine
    {
        public CargoFlightRoutine(ApplicationDbContext context, Flight flight) : base(context, flight) {}

        protected override bool LoadSpecificPayload()
        {
            int totalWeight = _context.BaggageItems.Where(b => b.FlightId == _flight.Id).Sum(b => (int?)b.Weight) ?? 0;
            
            if (totalWeight > _flight.BaggageLimitKg && _flight.BaggageLimitKg > 0)
            {
                RoutineLogs.Add($"❌ OVERWEIGHT! {totalWeight}kg loaded, max is {_flight.BaggageLimitKg}kg.");
                return false;
            }
            
            RoutineLogs.Add($"✅ 📦 Loading {totalWeight}kg cargo pallets.");
            RoutineLogs.Add("✅ ⚖️ Weight distribution nominal.");
            return true;
        }
    }
}
