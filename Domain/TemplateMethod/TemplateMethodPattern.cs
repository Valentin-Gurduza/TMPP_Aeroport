using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.TemplateMethod
{
    // 1. Abstract Class containing the Template Method
    public abstract class FlightPreflightRoutine
    {
        public List<string> RoutineLogs { get; } = new List<string>();

        // The Template Method
        public void ExecuteRoutine()
        {
            RoutineLogs.Add("--- Starting Pre-flight Routine ---");
            CheckFuel();
            CheckSystems();
            LoadSpecificPayload(); // Specialized step
            BriefCrew();
            RoutineLogs.Add("--- Pre-flight Routine Complete ---");
        }

        // Standard steps with default implementations
        protected void CheckFuel()
        {
            RoutineLogs.Add("✅ Checking fuel levels. Nominal.");
        }

        protected void CheckSystems()
        {
            RoutineLogs.Add("✅ Checking avionics and hydraulics. Systems GO.");
        }

        protected void BriefCrew()
        {
            RoutineLogs.Add("✅ Briefing crew on weather and flight plan.");
        }

        // Abstract step that MUST be implemented by subclasses
        protected abstract void LoadSpecificPayload();
    }

    // 2. Concrete Class 1
    public class PassengerFlightRoutine : FlightPreflightRoutine
    {
        protected override void LoadSpecificPayload()
        {
            RoutineLogs.Add("👥 Boarding passengers and loading luggage into hold.");
            RoutineLogs.Add("🍲 Ensuring catering supplies are onboard.");
        }
    }

    // 3. Concrete Class 2
    public class CargoFlightRoutine : FlightPreflightRoutine
    {
        protected override void LoadSpecificPayload()
        {
            RoutineLogs.Add("📦 Loading heavy cargo pallets.");
            RoutineLogs.Add("⚖️ Checking weight distribution and center of gravity limits.");
        }
    }
}
