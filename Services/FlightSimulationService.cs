using Microsoft.AspNetCore.SignalR;
using TMPP_Aeroport.Hubs;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;
using TMPP_Aeroport.Domain.ObjectPool;
using TMPP_Aeroport.Domain.Strategy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TMPP_Aeroport.Services
{
    public class FlightSimulationService : BackgroundService
    {
        private readonly ILogger<FlightSimulationService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<FlightHub> _hubContext;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly GroundVehiclePool _vehiclePool;

        private static int _globalSpeedMultiplier = 1;
        public static int GlobalSpeedMultiplier 
        { 
            get => Volatile.Read(ref _globalSpeedMultiplier); 
            set => Interlocked.Exchange(ref _globalSpeedMultiplier, value); 
        }
        private static readonly int TICK_INTERVAL_MS = 1000;
        
        public static DateTime VirtualTime { get; private set; }

        private List<SimulatedFlight> _activeFlights = new List<SimulatedFlight>();

        // Feature 3: Weather
        public static IWeatherStrategy CurrentWeather { get; private set; } = new ClearWeatherStrategy();
        private static readonly Random _rng = new Random();
        private int _weatherTickCount = 0;
        private const int WEATHER_CHANGE_INTERVAL = 600; // change every ~10 virtual minutes at 1x

        public FlightSimulationService(ILogger<FlightSimulationService> logger, IServiceScopeFactory scopeFactory,
            IHubContext<FlightHub> hubContext, IHostApplicationLifetime appLifetime, GroundVehiclePool vehiclePool)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _appLifetime = appLifetime;
            _vehiclePool = vehiclePool;
            VirtualTime = DateTime.Now;
            
            _appLifetime.ApplicationStopping.Register(SaveState);
        }

        public GroundVehiclePool GetVehiclePool() => _vehiclePool;
        public static void ForceWeather(IWeatherStrategy weather) { CurrentWeather = weather; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Global Flight Simulation Service is starting.");

            // Initial load of flights
            await InitializeFlightsAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TICK_INTERVAL_MS, stoppingToken);
                await TickAsync();
            }

            _logger.LogInformation("Global Flight Simulation Service is stopping.");
        }

        public static readonly Dictionary<string, (double Lat, double Lng)> Airports = new Dictionary<string, (double Lat, double Lng)>
        {
            { "Bucharest", (44.5722, 26.1022) },
            { "Paris", (49.0097, 2.5479) },
            { "Frankfurt", (50.0379, 8.5622) },
            { "London", (51.4700, -0.4543) },
            { "Amsterdam", (52.3105, 4.7683) },
            { "Rome", (41.7999, 12.2462) },
            { "Madrid", (40.4983, -3.5676) },
            { "Berlin", (52.3667, 13.5033) },
            { "Vienna", (48.1103, 16.5697) },
            { "Munich", (48.3538, 11.7861) },
            { "Lisbon", (38.7742, -9.1342) },
            { "Warsaw", (52.1657, 20.9671) },
            { "Athens", (37.9364, 23.9445) },
            { "Copenhagen", (55.6180, 12.6560) },
            { "Zurich", (47.4647, 8.5492) }
        };

        // --- ATC COMMANDS ---
        public void ApproveTakeoff(string flightNumber)
        {
            var f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
            if (f != null) 
            {
                f.TakeoffCleared = true;
                f.Status = "Cleared for Takeoff";
            }
        }

        public void ApproveLanding(string flightNumber)
        {
            var f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
            if (f != null)
            {
                f.LandingCleared = true;
                f.Status = "Cleared for Landing";
            }
        }

        public void ReturnToOrigin(string flightNumber)
        {
            var f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
            if (f != null && (f.Status == "Airborne" || f.Status.Contains("Holding")))
            {
                // The new Dest is the old Origin name
                var oldOriginName = f.OriginName;
                f.OriginName = f.DestName;
                f.DestName = oldOriginName;

                // The new Origin is the Current Position!
                f.OriginLat = f.CurrentLat;
                f.OriginLng = f.CurrentLng;
                
                // The new Dest coordinates
                var originalApt = Airports[f.DestName];
                f.DestLat = originalApt.Lat;
                f.DestLng = originalApt.Lng;

                double distanceKm = CalculateDistance(f.OriginLat, f.OriginLng, f.DestLat, f.DestLng);
                f.TotalTimeHours = distanceKm / 900.0;
                f.Progress = 0;
                f.ActualDepartureTime = VirtualTime;
                
                f.Status = "Airborne";
                f.TakeoffCleared = true;
                f.LandingCleared = false;
            }
        }

        public void DivertToNearest(string flightNumber)
        {
            var f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
            if (f != null && (f.Status == "Airborne" || f.Status.Contains("Holding")))
            {
                string nearestName = f.DestName;
                double minDistance = double.MaxValue;

                foreach (var apt in Airports)
                {
                    if (apt.Key == f.DestName || apt.Key == f.OriginName) continue; // Don't divert to current dest or origin
                    
                    double dist = CalculateDistance(f.CurrentLat, f.CurrentLng, apt.Value.Lat, apt.Value.Lng);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestName = apt.Key;
                    }
                }

                // Divert
                var dest = Airports[nearestName];
                f.OriginLat = f.CurrentLat;
                f.OriginLng = f.CurrentLng;
                f.OriginName = "Divert Origin";
                f.DestLat = dest.Lat;
                f.DestLng = dest.Lng;
                f.DestName = nearestName;

                f.TotalTimeHours = minDistance / 900.0;
                f.Progress = 0;
                f.ActualDepartureTime = VirtualTime;
                
                f.Status = "Airborne";
                f.TakeoffCleared = true;
                f.LandingCleared = false;
            }
        }

        public IEnumerable<SimulatedFlight> GetPendingRequests()
        {
            return _activeFlights.Where(f => f.Status.Contains("Awaiting Takeoff") || f.Status.Contains("Holding Pattern")).ToList();
        }
        
        public IEnumerable<SimulatedFlight> GetActiveFlights()
        {
            return _activeFlights.ToList();
        }
        // --------------------

        private async Task InitializeFlightsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var dbFlights = await dbContext.Flights.Include(f => f.Aircraft).ToListAsync();

            // Load Game State if exists
            bool loadedSave = false;
            string saveFilePath = "simulation_savegame.json";
            
            if (System.IO.File.Exists(saveFilePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(saveFilePath);
                    var saveData = System.Text.Json.JsonSerializer.Deserialize<SimulationSaveData>(json);
                    
                    if (saveData != null)
                    {
                        VirtualTime = saveData.VirtualTime;
                        GlobalSpeedMultiplier = saveData.GlobalSpeedMultiplier;
                        _activeFlights = saveData.ActiveFlights ?? new List<SimulatedFlight>();
                        loadedSave = true;
                        _logger.LogInformation($"Successfully loaded simulation state. VirtualTime: {VirtualTime}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load simulation_savegame.json. Starting fresh.");
                }
            }

            foreach (var f in dbFlights)
            {
                // If we loaded a save and this flight is already tracked, skip adding it
                if (loadedSave && _activeFlights.Any(sf => sf.FlightNumber == f.FlightNumber))
                    continue;

                // Assign a dummy origin just for simulation (OTP if destination is not OTP)
                var originName = f.Destination.Contains("Bucharest") ? "Paris" : "Bucharest";
                var destName = "Paris"; // Default fallback
                
                foreach (var apt in Airports.Keys)
                {
                    if (f.Destination.Contains(apt)) destName = apt;
                }

                if (!Airports.ContainsKey(originName)) originName = "Bucharest";
                if (!Airports.ContainsKey(destName)) destName = "Paris";

                var origin = Airports[originName];
                var dest = Airports[destName];

                double distanceKm = CalculateDistance(origin.Lat, origin.Lng, dest.Lat, dest.Lng);
                double defaultSpeedKmh = 900.0;
                double totalTimeHours = distanceKm / defaultSpeedKmh;

                int totalPax = f.MaxCapacity > 0 ? f.MaxCapacity : (_rng.Next(60, 200));
                var simFlight = new SimulatedFlight
                {
                    Id = f.Id,
                    FlightNumber = f.FlightNumber,
                    OriginName = originName,
                    DestName = destName,
                    OriginLat = origin.Lat,
                    OriginLng = origin.Lng,
                    DestLat = dest.Lat,
                    DestLng = dest.Lng,
                    CurrentLat = origin.Lat,
                    CurrentLng = origin.Lng,
                    Status = f.Status,
                    TotalTimeHours = totalTimeHours,
                    DepartureTime = f.DepartureTime,
                    Progress = 0,
                    AircraftModel = f.Aircraft?.Model ?? "A320",
                    TotalPassengers = totalPax,
                    FuelPercent = 100.0
                };

                _activeFlights.Add(simFlight);
            }
        }

        private async Task TickAsync()
        {
            VirtualTime = VirtualTime.AddSeconds((TICK_INTERVAL_MS / 1000.0) * GlobalSpeedMultiplier);
            
            await _hubContext.Clients.All.SendAsync("TimeUpdate", new { 
                VirtualTime = VirtualTime.ToString("dd MMM yyyy, HH:mm:ss"),
                Speed = GlobalSpeedMultiplier,
                Weather = CurrentWeather.Description,
                WeatherType = CurrentWeather.WeatherType.ToString()
            });

            // Feature 3: Random weather change every ~10 virtual minutes
            _weatherTickCount++;
            if (_weatherTickCount >= WEATHER_CHANGE_INTERVAL)
            {
                _weatherTickCount = 0;
                var roll = _rng.Next(0, 100);
                IWeatherStrategy newWeather;
                if (roll < 15) newWeather = new StormWeatherStrategy();
                else if (roll < 35) newWeather = new FogWeatherStrategy();
                else newWeather = new ClearWeatherStrategy();

                if (newWeather.WeatherType != CurrentWeather.WeatherType)
                {
                    CurrentWeather = newWeather;
                    await _hubContext.Clients.All.SendAsync("WeatherChanged", new {
                        Description = CurrentWeather.Description,
                        Type = CurrentWeather.WeatherType.ToString(),
                        CanLand = CurrentWeather.CanLand
                    });
                    if (!CurrentWeather.CanLand)
                    {
                        await _hubContext.Clients.All.SendAsync("SecurityAlert", new {
                            CheckpointId = "WEATHER",
                            Message = $"⛈️ RUNWAY CLOSED: {CurrentWeather.Description}",
                            Level = "CRITICAL"
                        });
                    }
                }
            }

            bool anyStateChanged = false;
            var changedFlights = new List<(int FlightId, string NewStatus)>();

            foreach (var f in _activeFlights)
            {
                string oldStatus = f.Status;
                
                DateTime arrivalTime = f.DepartureTime.AddHours(f.TotalTimeHours);
                DateTime boardingTime = f.DepartureTime.AddMinutes(-30);

                if (VirtualTime < boardingTime)
                {
                    f.Status = "Scheduled";
                    f.Progress = 0;
                    f.CurrentLat = f.OriginLat;
                    f.CurrentLng = f.OriginLng;
                    f.FuelPercent = 100.0;
                    f.BoardedCount = 0;
                    f.BoardingStarted = false;
                    f.ServicingStarted = false;
                    f.ServicingComplete = false;
                }
                else if (VirtualTime >= boardingTime && VirtualTime < f.DepartureTime)
                {
                    // Feature 4: Interactive Boarding via Iterator pattern
                    if (!f.BoardingStarted)
                    {
                        f.BoardingStarted = true;
                        f.BoardedCount = 0;
                        f.Status = "Boarding";
                    }
                    else
                    {
                        // Board ~5 passengers per tick (at 1x speed)
                        int boardRate = Math.Max(1, 5 * GlobalSpeedMultiplier);
                        f.BoardedCount = Math.Min(f.TotalPassengers, f.BoardedCount + boardRate);
                        if (f.BoardedCount >= f.TotalPassengers)
                            f.Status = "Boarding Complete";
                        else
                            f.Status = "Boarding";
                    }
                    f.Progress = 0;
                    f.CurrentLat = f.OriginLat;
                    f.CurrentLng = f.OriginLng;
                }
                else if (VirtualTime >= f.DepartureTime && f.Progress < 1.0 && VirtualTime < arrivalTime)
                {
                    if (!f.TakeoffCleared)
                    {
                        // Feature 3: Check weather for takeoff
                        if (!CurrentWeather.CanTakeoff)
                            f.Status = "Awaiting Takeoff - Weather Hold";
                        else
                            f.Status = "Awaiting Takeoff Clearance";
                        f.Progress = 0;
                        f.CurrentLat = f.OriginLat;
                        f.CurrentLng = f.OriginLng;
                        f.DepartureTime = VirtualTime;
                    }
                    else
                    {
                        f.Status = "Airborne";
                        if (!f.ActualDepartureTime.HasValue) f.ActualDepartureTime = VirtualTime;

                        TimeSpan elapsed = VirtualTime - f.ActualDepartureTime.Value;
                        double flightDurationSeconds = f.TotalTimeHours * 3600;
                        f.Progress = elapsed.TotalSeconds / flightDurationSeconds;
                        if (f.Progress > 1.0) f.Progress = 1.0;

                        f.CurrentLat = f.OriginLat + (f.DestLat - f.OriginLat) * f.Progress;
                        f.CurrentLng = f.OriginLng + (f.DestLng - f.OriginLng) * f.Progress;

                        // Feature 3: Burn fuel (1% per virtual hour = ~1/3600 per second)
                        double burnRate = 0.001 * GlobalSpeedMultiplier * CurrentWeather.FuelBurnMultiplier;
                        f.FuelPercent = Math.Max(0, f.FuelPercent - burnRate);
                    }
                }
                else if (f.Progress >= 1.0)
                {
                    if (!f.LandingCleared)
                    {
                        if (!f.HoldingStartTime.HasValue) f.HoldingStartTime = VirtualTime;
                        TimeSpan holdingElapsed = VirtualTime - f.HoldingStartTime.Value;

                        // Feature 3: Burn extra fuel in hold; storm blocks landing
                        if (!CurrentWeather.CanLand)
                            f.Status = "Holding Pattern (Weather - Storm)";
                        else
                            f.Status = "Holding Pattern (Awaiting Landing)";

                        double holdBurn = 0.002 * GlobalSpeedMultiplier * CurrentWeather.FuelBurnMultiplier;
                        f.FuelPercent = Math.Max(0, f.FuelPercent - holdBurn);

                        // Feature 3: Emergency - auto divert if fuel < 10%
                        if (f.FuelPercent <= 10.0 && !f.EmergencyDeclared)
                        {
                            f.EmergencyDeclared = true;
                            await _hubContext.Clients.All.SendAsync("SecurityAlert", new {
                                CheckpointId = "ATC-EMERGENCY",
                                Message = $"🚨 EMERGENCY: {f.FlightNumber} FUEL CRITICAL ({f.FuelPercent:F0}%) - AUTO DIVERT ACTIVATED",
                                Level = "EMERGENCY"
                            });
                            DivertToNearest(f.FlightNumber);
                        }

                        f.CurrentLat = f.DestLat + Math.Sin(holdingElapsed.TotalSeconds * 0.1) * 0.05;
                        f.CurrentLng = f.DestLng + Math.Cos(holdingElapsed.TotalSeconds * 0.1) * 0.05;
                    }
                    else
                    {
                        if (!f.ActualArrivalTime.HasValue)
                        {
                            f.ActualArrivalTime = VirtualTime;
                            // Feature 5: Smart Gate Allocation on landing
                            var allocator = new SmartGateAllocator();
                            var occupiedGates = _activeFlights.Where(x => !string.IsNullOrEmpty(x.AssignedGate)).Select(x => x.AssignedGate).ToList();
                            f.AssignedGate = allocator.AllocateGate(f.AircraftModel, occupiedGates);

                            // Feature 2: Dispatch GSE vehicles
                            _vehiclePool.AcquireVehicle(VehicleType.FuelTruck, f.FlightNumber, VirtualTime);
                            _vehiclePool.AcquireVehicle(VehicleType.CateringTruck, f.FlightNumber, VirtualTime);
                            _vehiclePool.AcquireVehicle(VehicleType.Tug, f.FlightNumber, VirtualTime);
                            f.ServicingStarted = true;

                            await _hubContext.Clients.All.SendAsync("FlightStateChanged", new {
                                FlightNumber = f.FlightNumber,
                                OldStatus = "Cleared for Landing",
                                NewStatus = $"Gate {f.AssignedGate} - Servicing",
                                Origin = f.OriginName,
                                Destination = f.DestName
                            });
                        }

                        DateTime deplaningEndTimeReal = f.ActualArrivalTime.Value.AddMinutes(45);

                        if (VirtualTime < deplaningEndTimeReal)
                        {
                            f.Status = "Landed";
                            f.Progress = 1.0;
                            f.CurrentLat = f.DestLat;
                            f.CurrentLng = f.DestLng;

                            // Feature 2: Complete servicing when 30 minutes pass
                            if (!f.ServicingComplete && VirtualTime >= f.ActualArrivalTime.Value.AddMinutes(30))
                            {
                                f.ServicingComplete = true;
                                _vehiclePool.ReleaseVehiclesForFlight(f.FlightNumber);
                                f.FuelPercent = 100.0; // refueled!
                            }
                        }
                        else
                        {
                            // Reset for return flight
                            f.DepartureTime = deplaningEndTimeReal.AddHours(1.25);
                            f.ActualDepartureTime = null;
                            f.ActualArrivalTime = null;
                            f.HoldingStartTime = null;
                            f.TakeoffCleared = false;
                            f.LandingCleared = false;
                            f.ServicingStarted = false;
                            f.ServicingComplete = false;
                            f.AssignedGate = string.Empty;
                            f.EmergencyDeclared = false;
                            f.BoardedCount = 0;
                            f.BoardingStarted = false;
                            f.FuelPercent = 100.0;

                            var tempLat = f.OriginLat; var tempLng = f.OriginLng; var tempName = f.OriginName;
                            f.OriginLat = f.DestLat; f.OriginLng = f.DestLng; f.OriginName = f.DestName;
                            f.DestLat = tempLat; f.DestLng = tempLng; f.DestName = tempName;

                            f.Status = "Scheduled";
                            f.Progress = 0;
                            f.CurrentLat = f.OriginLat;
                            f.CurrentLng = f.OriginLng;
                        }
                    }
                }

                if (oldStatus != f.Status)
                {
                    anyStateChanged = true;
                    await _hubContext.Clients.All.SendAsync("FlightStateChanged", new {
                        FlightNumber = f.FlightNumber,
                        OldStatus = oldStatus,
                        NewStatus = f.Status,
                        Origin = f.OriginName,
                        Destination = f.DestName
                    });
                    changedFlights.Add((f.Id, f.Status));
                }

                // Feature 4: Push boarding progress every tick during boarding
                if (f.Status == "Boarding" && f.BoardingStarted)
                {
                    await _hubContext.Clients.All.SendAsync("BoardingProgress", new {
                        FlightNumber = f.FlightNumber,
                        Boarded = f.BoardedCount,
                        Total = f.TotalPassengers,
                        Gate = f.AssignedGate
                    });
                }
            }

            if (changedFlights.Any())
            {
                await UpdateDatabaseStatusesAsync(changedFlights);
            }

            // Always broadcast radar positions every tick (for smooth animation)
            var radarData = _activeFlights.Where(f => f.Status == "Airborne").Select(f => new {
                f.FlightNumber,
                f.CurrentLat,
                f.CurrentLng,
                f.OriginName,
                f.DestName,
                f.OriginLat,
                f.OriginLng,
                f.DestLat,
                f.DestLng,
                f.Status
            });
            await _hubContext.Clients.All.SendAsync("RadarUpdate", radarData);
            
            // Also broadcast full board update if state changed
            if (anyStateChanged)
            {
                await _hubContext.Clients.All.SendAsync("BoardUpdate", _activeFlights);
            }
        }

        private async Task UpdateDatabaseStatusesAsync(List<(int FlightId, string NewStatus)> changedFlights)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var flightIds = changedFlights.Select(x => x.FlightId).ToList();
            var flightsInDb = await dbContext.Flights.Where(f => flightIds.Contains(f.Id)).ToListAsync();

            foreach (var flight in flightsInDb)
            {
                var newStatus = changedFlights.First(x => x.FlightId == flight.Id).NewStatus;
                flight.Status = newStatus;

                // Observer Pattern Trigger
                var subject = new TMPP_Aeroport.Domain.Observer.FlightStatusSubject(flight.FlightNumber);
                subject.Attach(new TMPP_Aeroport.Domain.Observer.PassengerNotifier(dbContext));
                subject.Attach(new TMPP_Aeroport.Domain.Observer.DisplayBoardUpdater(dbContext));
                
                subject.Status = newStatus; // This will call Notify() and write AuditLogs
            }
            
            if (flightsInDb.Any())
            {
                await dbContext.SaveChangesAsync();
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double r = 6371; // km
            double p = Math.PI / 180;
            double a = 0.5 - Math.Cos((lat2 - lat1) * p) / 2 + 
                       Math.Cos(lat1 * p) * Math.Cos(lat2 * p) * 
                       (1 - Math.Cos((lon2 - lon1) * p)) / 2;
            return 2 * r * Math.Asin(Math.Sqrt(a));
        }

        private void SaveState()
        {
            try
            {
                var saveData = new SimulationSaveData
                {
                    VirtualTime = VirtualTime,
                    GlobalSpeedMultiplier = GlobalSpeedMultiplier,
                    ActiveFlights = _activeFlights
                };

                string json = System.Text.Json.JsonSerializer.Serialize(saveData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText("simulation_savegame.json", json);
                _logger.LogInformation("Simulation state saved successfully to simulation_savegame.json.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save simulation state.");
            }
        }
    }

    public class SimulationSaveData
    {
        public DateTime VirtualTime { get; set; }
        public int GlobalSpeedMultiplier { get; set; }
        public List<SimulatedFlight> ActiveFlights { get; set; } = new List<SimulatedFlight>();
    }

    public class SimulatedFlight
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string OriginName { get; set; } = string.Empty;
        public string DestName { get; set; } = string.Empty;
        public double OriginLat { get; set; }
        public double OriginLng { get; set; }
        public double DestLat { get; set; }
        public double DestLng { get; set; }
        public double CurrentLat { get; set; }
        public double CurrentLng { get; set; }
        public string Status { get; set; } = string.Empty;
        public double TotalTimeHours { get; set; }
        public DateTime DepartureTime { get; set; }
        public double Progress { get; set; }
        public string AircraftModel { get; set; } = string.Empty;

        // ATC Clearance Tracking
        public bool TakeoffCleared { get; set; } = false;
        public bool LandingCleared { get; set; } = false;
        public DateTime? ActualDepartureTime { get; set; }
        public DateTime? ActualArrivalTime { get; set; }
        public DateTime? HoldingStartTime { get; set; }

        // Feature 3: Fuel (virtual, in %)
        public double FuelPercent { get; set; } = 100.0;
        public bool EmergencyDeclared { get; set; } = false;

        // Feature 2: GSE - Ground Servicing
        public bool ServicingStarted { get; set; } = false;
        public bool ServicingComplete { get; set; } = false;
        public string AssignedGate { get; set; } = string.Empty;

        // Feature 4: Interactive Boarding
        public int TotalPassengers { get; set; } = 0;
        public int BoardedCount { get; set; } = 0;
        public bool BoardingStarted { get; set; } = false;
    }
}
