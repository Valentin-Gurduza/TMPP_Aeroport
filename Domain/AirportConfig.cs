using System.Collections.Generic;

namespace TMPP_Aeroport.Domain
{
    public static class AirportConfig
    {
        public static List<RunwayModel> Runways { get; } = new List<RunwayModel>
        {
            new RunwayModel { Code = "08R", Name = "Runway 08 Right", Status = "Available" },
            new RunwayModel { Code = "08L", Name = "Runway 08 Left", Status = "Available" },
            new RunwayModel { Code = "26R", Name = "Runway 26 Right", Status = "Available" },
            new RunwayModel { Code = "26L", Name = "Runway 26 Left", Status = "Available" },
            new RunwayModel { Code = "14", Name = "Runway 14", Status = "Maintenance" }
        };

        public static List<FlightTypeModel> FlightTypes { get; } = new List<FlightTypeModel>
        {
            new FlightTypeModel { Value = "passenger", Name = "Passenger Flight" },
            new FlightTypeModel { Value = "cargo", Name = "Cargo Flight" }
        };

        public static List<FlightStatusOption> StatusOptions { get; } = new List<FlightStatusOption>
        {
            new FlightStatusOption { Value = "Boarding", Name = "BOARDING", Color = "amber" },
            new FlightStatusOption { Value = "Delayed", Name = "DELAYED", Color = "red" },
            new FlightStatusOption { Value = "Airborne", Name = "AIRBORNE", Color = "emerald" },
            new FlightStatusOption { Value = "Landed", Name = "LANDED", Color = "slate" },
            new FlightStatusOption { Value = "Cancelled", Name = "CANCELLED", Color = "red" },
            new FlightStatusOption { Value = "Scheduled", Name = "SCHEDULED", Color = "blue" }
        };
        
        public static bool IsValidTransition(string oldStatus, string newStatus)
        {
            if (oldStatus == newStatus) return false;
            
            // Simplistic rule map
            var validTransitions = new Dictionary<string, List<string>>
            {
                { "Draft", new List<string> { "Scheduled", "Cancelled" } },
                { "Scheduled", new List<string> { "Boarding", "Delayed", "Cancelled" } },
                { "Boarding", new List<string> { "Airborne", "Delayed", "Cancelled" } },
                { "Delayed", new List<string> { "Boarding", "Airborne", "Cancelled" } },
                { "Airborne", new List<string> { "Landed", "Diverted" } },
                { "Landed", new List<string> { "Completed" } },
                { "Unknown", new List<string> { "Scheduled", "Boarding", "Airborne", "Landed", "Cancelled", "Delayed" } }
            };

            if (!validTransitions.ContainsKey(oldStatus)) return true; // Default allow if not restricted
            
            return validTransitions[oldStatus].Contains(newStatus);
        }
    }

    public class RunwayModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class FlightTypeModel
    {
        public string Value { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class FlightStatusOption
    {
        public string Value { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}
