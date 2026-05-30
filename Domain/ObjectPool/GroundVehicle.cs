namespace TMPP_Aeroport.Domain.ObjectPool
{
    public enum VehicleType { FuelTruck, CateringTruck, Tug }
    public enum VehicleStatus { Available, InUse, Maintenance }

    public class GroundVehicle
    {
        public string Id { get; set; } = string.Empty;
        public VehicleType Type { get; set; }
        public VehicleStatus Status { get; set; } = VehicleStatus.Available;
        public string? AssignedFlight { get; set; }
        public DateTime? AssignedAt { get; set; }
        public int ServiceDurationMinutes { get; set; } // virtual minutes to complete

        public string TypeLabel => Type switch
        {
            VehicleType.FuelTruck => "⛽ Fuel Truck",
            VehicleType.CateringTruck => "🍽️ Catering Truck",
            VehicleType.Tug => "🚜 Tug / Pushback",
            _ => "Vehicle"
        };
    }
}
