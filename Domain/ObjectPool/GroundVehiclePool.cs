using System.Collections.Concurrent;

namespace TMPP_Aeroport.Domain.ObjectPool
{
    // Object Pool Pattern - manages a fixed fleet of ground support vehicles
    public class GroundVehiclePool
    {
        private readonly List<GroundVehicle> _allVehicles = new();
        private readonly object _lock = new object();

        public GroundVehiclePool()
        {
            // Initialize fleet: 3 fuel trucks, 3 catering, 4 tugs
            for (int i = 1; i <= 3; i++)
                _allVehicles.Add(new GroundVehicle { Id = $"FT-{i:D2}", Type = VehicleType.FuelTruck, ServiceDurationMinutes = 15 });
            for (int i = 1; i <= 3; i++)
                _allVehicles.Add(new GroundVehicle { Id = $"CT-{i:D2}", Type = VehicleType.CateringTruck, ServiceDurationMinutes = 20 });
            for (int i = 1; i <= 4; i++)
                _allVehicles.Add(new GroundVehicle { Id = $"TG-{i:D2}", Type = VehicleType.Tug, ServiceDurationMinutes = 10 });
        }

        public IReadOnlyList<GroundVehicle> AllVehicles 
        {
            get
            {
                lock (_lock)
                {
                    return _allVehicles.ToList().AsReadOnly();
                }
            }
        }

        public GroundVehicle? AcquireVehicle(VehicleType type, string flightNumber, DateTime virtualNow)
        {
            lock (_lock)
            {
                var vehicle = _allVehicles.FirstOrDefault(v => v.Type == type && v.Status == VehicleStatus.Available);
                if (vehicle != null)
                {
                    vehicle.Status = VehicleStatus.InUse;
                    vehicle.AssignedFlight = flightNumber;
                    vehicle.AssignedAt = virtualNow;
                }
                return vehicle;
            }
        }

        public void ReleaseVehicle(string vehicleId)
        {
            lock (_lock)
            {
                var vehicle = _allVehicles.FirstOrDefault(v => v.Id == vehicleId);
                if (vehicle != null)
                {
                    vehicle.Status = VehicleStatus.Available;
                    vehicle.AssignedFlight = null;
                    vehicle.AssignedAt = null;
                }
            }
        }

        public void ReleaseVehiclesForFlight(string flightNumber)
        {
            lock (_lock)
            {
                foreach (var v in _allVehicles.Where(v => v.AssignedFlight == flightNumber))
                {
                    v.Status = VehicleStatus.Available;
                    v.AssignedFlight = null;
                    v.AssignedAt = null;
                }
            }
        }

        public List<GroundVehicle> GetVehiclesForFlight(string flightNumber)
        {
            lock (_lock)
            {
                return _allVehicles.Where(v => v.AssignedFlight == flightNumber).ToList();
            }
        }

        public int AvailableCount(VehicleType type)
        {
            lock (_lock)
            {
                return _allVehicles.Count(v => v.Type == type && v.Status == VehicleStatus.Available);
            }
        }
    }
}
