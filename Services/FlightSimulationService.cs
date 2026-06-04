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
        private readonly object _flightLock = new object();

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
            { "Chisinau", (46.9277, 28.9306) },
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
            SimulatedFlight? f;
            lock (_flightLock)
            {
                f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
                if (f != null) 
                {
                    f.TakeoffCleared = true;
                    f.Status = "Cleared for Takeoff";
                }
            }
        }

        public void ApproveLanding(string flightNumber)
        {
            SimulatedFlight? f;
            lock (_flightLock)
            {
                f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
                if (f != null)
                {
                    f.LandingCleared = true;
                    f.Status = "Cleared for Landing";
                }
            }
        }

        public void ReturnToOrigin(string flightNumber)
        {
            SimulatedFlight? f;
            lock (_flightLock)
            {
                f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
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
                if (Airports.ContainsKey(f.DestName))
                {
                    var originalApt = Airports[f.DestName];
                    f.DestLat = originalApt.Lat;
                    f.DestLng = originalApt.Lng;
                }
                else
                {
                    // Fallback to Bucharest if the old origin was Divert Origin or unknown
                    var fallbackApt = Airports["Bucharest"];
                    f.DestName = "Bucharest";
                    f.DestLat = fallbackApt.Lat;
                    f.DestLng = fallbackApt.Lng;
                }

                double distanceKm = CalculateDistance(f.OriginLat, f.OriginLng, f.DestLat, f.DestLng);
                f.TotalTimeHours = distanceKm / 900.0;
                f.Progress = 0;
                f.ActualDepartureTime = VirtualTime;
                
                f.Status = "Airborne";
                f.TakeoffCleared = true;
                f.LandingCleared = false;
            }
            }
        }

        public void DivertToNearest(string flightNumber)
        {
            SimulatedFlight? f;
            lock (_flightLock)
            {
                f = _activeFlights.FirstOrDefault(x => x.FlightNumber == flightNumber);
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
        }

        public IEnumerable<SimulatedFlight> GetPendingRequests()
        {
            lock (_flightLock)
            {
                return _activeFlights.Where(f => f.Status.Contains("Awaiting Takeoff") || f.Status.Contains("Holding Pattern")).ToList();
            }
        }
        
        public IEnumerable<SimulatedFlight> GetActiveFlights()
        {
            lock (_flightLock)
            {
                return _activeFlights.ToList();
            }
        }
        
        public async Task SyncFlightAsync(int flightId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var flightDb = await dbContext.Flights
                .Include(f => f.Aircraft)
                .FirstOrDefaultAsync(f => f.Id == flightId);

            if (flightDb == null || flightDb.Status == "Completed" || flightDb.Status == "Cancelled")
            {
                lock (_flightLock)
                {
                    _activeFlights.RemoveAll(f => f.Id == flightId);
                }
                return;
            }

            SimulatedFlight? existingSimFlight;
            lock (_flightLock)
            {
                existingSimFlight = _activeFlights.FirstOrDefault(f => f.Id == flightId);
            }

            if (existingSimFlight != null)
            {
                // Update existing
                existingSimFlight.FlightNumber = flightDb.FlightNumber;
                existingSimFlight.DepartureTime = flightDb.DepartureTime;
                existingSimFlight.Status = flightDb.Status;
                existingSimFlight.AssignedGate = flightDb.Gate ?? "";
                if (flightDb.ArrivalTime.HasValue) existingSimFlight.ActualArrivalTime = flightDb.ArrivalTime;
            }
            else
            {
                // Add new (cloned or newly created)
                await LoadFlightIntoSimulationAsync(flightDb, dbContext);
            }
        }
        // --------------------

        private async Task InitializeFlightsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Always start from a clean RAM state - ONLY load from DB, never from savegame file
            lock (_flightLock) { _activeFlights.Clear(); }

            // Delete any leftover savegame file to prevent corrupted state on next restart
            if (System.IO.File.Exists("simulation_savegame.json"))
                System.IO.File.Delete("simulation_savegame.json");

            // Load only active (non-completed) flights from DB
            var dbFlights = await dbContext.Flights
                .Include(f => f.Aircraft)
                .Where(f => f.Status != "Completed" && f.Status != "Cancelled")
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();

            var now = DateTime.Now;
            var staleFlights = dbFlights.Where(f => f.DepartureTime < now.AddHours(-4)).ToList();
            foreach (var sf in staleFlights)
            {
                sf.Status = "Cancelled"; // Mark old ghost flights as cancelled so they don't instantly spawn on radar
                dbFlights.Remove(sf);
            }
            
            if (staleFlights.Any())
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Cleaned up {staleFlights.Count} stale flights from previous sessions.");
            }

            _logger.LogInformation($"Initializing simulation with {dbFlights.Count} valid flights from database.");

            foreach (var f in dbFlights)
            {
                await LoadFlightIntoSimulationAsync(f, dbContext);
            }

            _logger.LogInformation($"Simulation initialized. {_activeFlights.Count} flights loaded into RAM.");
        }

        private async Task LoadFlightIntoSimulationAsync(TMPP_Aeroport.Models.Flight f, ApplicationDbContext dbContext)
        {
            var originName = string.IsNullOrEmpty(f.Origin) ? "Bucharest" : f.Origin;
            var destName = string.IsNullOrEmpty(f.Destination) ? "Paris" : f.Destination;
            
            string matchedOrigin = originName;
            string matchedDest = destName;

            foreach (var apt in Airports.Keys)
            {
                if (originName.Contains(apt)) matchedOrigin = apt;
                if (destName.Contains(apt)) matchedDest = apt;
            }

            originName = matchedOrigin;
            destName = matchedDest;

            if (!Airports.ContainsKey(originName)) originName = "Bucharest";
            if (!Airports.ContainsKey(destName)) destName = "Paris";
            
            if (originName == destName) 
            {
                if (originName == "Bucharest") destName = "Paris";
                else originName = "Bucharest";
            }

            var origin = Airports[originName];
            var dest = Airports[destName];

            double distanceKm = CalculateDistance(origin.Lat, origin.Lng, dest.Lat, dest.Lng);
            double defaultSpeedKmh = 900.0;
            double totalTimeHours = distanceKm / defaultSpeedKmh;

            int totalPax = f.MaxCapacity > 0 ? f.MaxCapacity : (_rng.Next(60, 200));
            
            // --- Passenger Automation ---
            var ticketCount = await dbContext.Tickets.CountAsync(t => t.FlightId == f.Id);
            if (ticketCount == 0)
            {
                int numTicketsToGenerate = _rng.Next((int)(totalPax * 0.7), totalPax + 1);
                var ticketsToInsert = new List<Ticket>();
                var bagsToInsert = new List<BaggageItem>();
                
                var seats = new List<string>();
                for(int r=1; r<=40; r++) {
                    for(char c='A'; c<='F'; c++) { seats.Add($"{r}{c}"); }
                }
                seats = seats.OrderBy(x => _rng.Next()).ToList();

                string[] firstNames = { "Alex", "Maria", "John", "Elena", "Andrew", "Anna", "Michael", "Joanna", "Stephen", "Diana", "Gabriel", "Christina" };
                string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };

                for (int i = 0; i < numTicketsToGenerate && i < seats.Count; i++)
                {
                    string randomName = $"{firstNames[_rng.Next(firstNames.Length)]} {lastNames[_rng.Next(lastNames.Length)]}";
                    var ticket = new Ticket
                    {
                        FlightId = f.Id,
                        PassengerName = randomName,
                        Price = _rng.Next(50, 500),
                        SeatNumber = seats[i],
                        TicketState = "Issued",
                        FareClass = _rng.Next(0, 10) > 8 ? "Business" : "Economy",
                        BaggageWeight = 0,
                        UserId = null
                    };
                    ticketsToInsert.Add(ticket);
                }
                await dbContext.Tickets.AddRangeAsync(ticketsToInsert);
                await dbContext.SaveChangesAsync();

                foreach (var t in ticketsToInsert)
                {
                    int numBags = _rng.Next(0, 3);
                    for (int b = 0; b < numBags; b++)
                    {
                        bagsToInsert.Add(new BaggageItem
                        {
                            FlightId = f.Id,
                            Weight = _rng.Next(5, 25),
                            Type = "Suitcase",
                            TagCode = $"BG-{f.FlightNumber}-{_rng.Next(10000, 99999)}",
                            BaggageStage = "PendingCheckIn",
                            SecurityStatus = "Pending"
                        });
                    }
                }
                if (bagsToInsert.Any())
                {
                    await dbContext.BaggageItems.AddRangeAsync(bagsToInsert);
                    await dbContext.SaveChangesAsync();
                }
            }

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
                FuelPercent = 100.0,
                
                PassengersGenerated = true,
                TotalTickets = await dbContext.Tickets.CountAsync(t => t.FlightId == f.Id),
                CheckedInCount = await dbContext.Tickets.CountAsync(t => t.FlightId == f.Id && t.TicketState != "Issued" && t.TicketState != "Cancelled"),
                BoardedCount = await dbContext.Tickets.CountAsync(t => t.FlightId == f.Id && t.TicketState == "Boarded"),
                TotalBags = await dbContext.BaggageItems.CountAsync(b => b.FlightId == f.Id),
                BagsLoadedCount = await dbContext.BaggageItems.CountAsync(b => b.FlightId == f.Id && b.BaggageStage == "LoadedOnAircraft")
            };

            lock (_flightLock)
            {
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

            _passengerSyncTickCount++;
            if (_passengerSyncTickCount >= 5)
            {
                _passengerSyncTickCount = 0;
                await SyncPassengerFlowAsync();
                await AdvanceBaggageStagesAsync();
            }

            _lifecycleSyncTickCount++;
            if (_lifecycleSyncTickCount >= 60)
            {
                _lifecycleSyncTickCount = 0;
                await ManageAirportLifecycleAsync();
            }

            bool anyStateChanged = false;
            var changedFlights = new List<(int FlightId, string NewStatus, string AssignedGate, DateTime? ActualArrivalTime)>();

            var stateChangesToSend = new List<object>();
            var boardingProgressToSend = new List<object>();

            lock (_flightLock)
            {
                foreach (var f in _activeFlights)
                {
                string oldStatus = f.Status;
                
                // --- Dynamic thresholds proportional to flight duration ---
                double flightMinutes = f.TotalTimeHours * 60.0;
                // Boarding window: at least 20 min, up to 25% of flight duration (max 90 min)
                double boardingWindowMinutes = Math.Clamp(flightMinutes * 0.25, 20.0, 90.0);
                // FIDS visibility: flight appears on board with min(180, 2x flight duration) min before departure
                double fidsWindowMinutes = Math.Min(180.0, flightMinutes * 2.0);

                DateTime arrivalTime = f.DepartureTime.AddHours(f.TotalTimeHours);
                DateTime boardingTime = f.DepartureTime.AddMinutes(-boardingWindowMinutes);

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
                    // Feature 4: Interactive Boarding synced with DB Tickets
                    if (!f.BoardingStarted)
                    {
                        f.BoardingStarted = true;
                        f.Status = "Boarding";
                        f.TotalPassengers = f.TotalTickets > 0 ? f.TotalTickets : f.TotalPassengers;
                    }
                    else
                    {
                        if (f.BoardedCount >= f.TotalPassengers && f.TotalPassengers > 0)
                            f.Status = "Boarding Complete";
                        else
                            f.Status = "Boarding";
                    }
                    f.Progress = 0;
                    f.CurrentLat = f.OriginLat;
                    f.CurrentLng = f.OriginLng;
                }
                else if (VirtualTime >= f.DepartureTime && f.Progress < 1.0)
                {
                    if (!f.TakeoffCleared)
                    {
                        // Auto-clear takeoff after 2 virtual minutes if weather allows, so planes don't get stuck forever
                        if (CurrentWeather.CanTakeoff && (VirtualTime - f.DepartureTime).TotalMinutes > 2)
                        {
                            f.TakeoffCleared = true;
                        }
                        else 
                        {
                            // Feature 3: Check weather for takeoff
                            if (!CurrentWeather.CanTakeoff)
                                f.Status = "Awaiting Takeoff - Weather Hold";
                            else
                                f.Status = "Awaiting Takeoff Clearance";
                            f.Progress = 0;
                            f.CurrentLat = f.OriginLat;
                            f.CurrentLng = f.OriginLng;
                        }
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
                            stateChangesToSend.Add(new {
                                SpecialEvent = "Emergency",
                                Message = $"🚨 EMERGENCY: {f.FlightNumber} FUEL CRITICAL ({f.FuelPercent:F0}%) - AUTO DIVERT ACTIVATED",
                                Level = "EMERGENCY"
                            });
                            DivertToNearest(f.FlightNumber);
                        }

                        f.CurrentLat = f.DestLat + Math.Sin(holdingElapsed.TotalSeconds * 0.1) * 0.5;
                        f.CurrentLng = f.DestLng + Math.Cos(holdingElapsed.TotalSeconds * 0.1) * 0.5;
                    }
                    else
                    {
                        if (!f.ActualArrivalTime.HasValue)
                        {
                            f.ActualArrivalTime = VirtualTime;
                            // Feature 5: Smart Gate Allocation on landing (Only if not assigned manually)
                            if (string.IsNullOrEmpty(f.AssignedGate))
                            {
                                var allocator = new SmartGateAllocator();
                                var occupiedGates = _activeFlights.Where(x => !string.IsNullOrEmpty(x.AssignedGate)).Select(x => x.AssignedGate).ToList();
                                f.AssignedGate = allocator.AllocateGate(f.AircraftModel, occupiedGates);
                            }

                            // Feature 2: Dispatch GSE vehicles
                            _vehiclePool.AcquireVehicle(VehicleType.FuelTruck, f.FlightNumber, VirtualTime);
                            _vehiclePool.AcquireVehicle(VehicleType.CateringTruck, f.FlightNumber, VirtualTime);
                            _vehiclePool.AcquireVehicle(VehicleType.Tug, f.FlightNumber, VirtualTime);
                            f.ServicingStarted = true;

                            stateChangesToSend.Add(new {
                                FlightNumber = f.FlightNumber,
                                OldStatus = "Cleared for Landing",
                                NewStatus = $"Gate {f.AssignedGate} - Servicing",
                                Origin = f.OriginName,
                                Destination = f.DestName
                            });
                        }

                        // Deplaning/servicing window: at least 15 min, up to 20% of flight duration (max 60 min)
                        double deplaningMinutes = Math.Clamp(flightMinutes * 0.20, 15.0, 60.0);
                        DateTime deplaningEndTimeReal = f.ActualArrivalTime.Value.AddMinutes(deplaningMinutes);

                        if (VirtualTime < deplaningEndTimeReal)
                        {
                            f.Status = "Landed";
                            f.Progress = 1.0;
                            f.CurrentLat = f.DestLat;
                            f.CurrentLng = f.DestLng;

                            // Feature 2: Complete servicing when vehicles finish
                            var assignedVehicles = _vehiclePool.GetVehiclesForFlight(f.FlightNumber);
                            // Cap servicing duration so it fits within the deplaning window
                            double maxDuration = Math.Min(deplaningMinutes * 0.8, 30.0);
                            if (assignedVehicles.Any())
                            {
                                maxDuration = Math.Min(assignedVehicles.Max(v => v.ServiceDurationMinutes), deplaningMinutes * 0.8);
                            }

                            if (!f.ServicingComplete && VirtualTime >= f.ActualArrivalTime.Value.AddMinutes(maxDuration))
                            {
                                f.ServicingComplete = true;
                                _vehiclePool.ReleaseVehiclesForFlight(f.FlightNumber);
                                f.FuelPercent = 100.0; // refueled!
                            }
                        }
                        else
                        {
                            // Feature: Garbage Collection target
                            f.Status = "Completed";
                        }
                    }
                }

                if (oldStatus != f.Status)
                {
                    anyStateChanged = true;
                    stateChangesToSend.Add(new {
                        FlightId = f.Id,
                        FlightNumber = f.FlightNumber,
                        OldStatus = oldStatus,
                        NewStatus = f.Status,
                        Origin = f.OriginName,
                        Destination = f.DestName
                    });
                    changedFlights.Add((f.Id, f.Status, f.AssignedGate, f.ActualArrivalTime));
                }

                // Feature 4: Push boarding progress every tick during boarding
                if (f.Status == "Boarding" && f.BoardingStarted)
                {
                    boardingProgressToSend.Add(new {
                        FlightNumber = f.FlightNumber,
                        Boarded = f.BoardedCount,
                        Total = f.TotalPassengers,
                        Gate = f.AssignedGate
                    });
                }
                }
            } // end of lock

            foreach (var msg in stateChangesToSend)
            {
                // Dynamic check since we mixed emergency payload in here for brevity
                var dmsg = msg as dynamic;
                try {
                    if (dmsg?.SpecialEvent == "Emergency")
                        await _hubContext.Clients.All.SendAsync("SecurityAlert", new { CheckpointId = "ATC-EMERGENCY", Message = dmsg.Message, Level = dmsg.Level });
                    else
                        await _hubContext.Clients.All.SendAsync("FlightStateChanged", msg);
                } catch { }
            }

            foreach (var bp in boardingProgressToSend)
            {
                await _hubContext.Clients.All.SendAsync("BoardingProgress", bp);
            }

            if (changedFlights.Any())
            {
                await UpdateDatabaseStatusesAsync(changedFlights);
            }

            // Always broadcast radar positions every tick (for smooth animation)
            IEnumerable<dynamic> radarData;
            lock (_flightLock)
            {
                radarData = _activeFlights.Where(f => f.Status == "Airborne" || f.Status.Contains("Holding")).Select(f => new {
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
                }).ToList();
            }
            await _hubContext.Clients.All.SendAsync("RadarUpdate", radarData);
            
            // Also broadcast full board update if state changed
            if (anyStateChanged)
            {
                List<SimulatedFlight> snapshot;
                lock (_flightLock)
                {
                    snapshot = _activeFlights.ToList();
                }
                await _hubContext.Clients.All.SendAsync("BoardUpdate", snapshot);
            }
        }

        private async Task UpdateDatabaseStatusesAsync(List<(int FlightId, string NewStatus, string AssignedGate, DateTime? ActualArrivalTime)> changedFlights)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var flightIds = changedFlights.Select(x => x.FlightId).ToList();
            var flightsInDb = await dbContext.Flights.Where(f => flightIds.Contains(f.Id)).ToListAsync();

            foreach (var flight in flightsInDb)
            {
                var update = changedFlights.First(x => x.FlightId == flight.Id);
                flight.Status = update.NewStatus;
                
                if (!string.IsNullOrEmpty(update.AssignedGate))
                    flight.Gate = update.AssignedGate;
                if (update.ActualArrivalTime.HasValue)
                    flight.ArrivalTime = update.ActualArrivalTime;

                // Observer Pattern Trigger
                var subject = new TMPP_Aeroport.Domain.Observer.FlightStatusSubject(flight.FlightNumber);
                subject.Attach(new TMPP_Aeroport.Domain.Observer.PassengerNotifier(dbContext));
                subject.Attach(new TMPP_Aeroport.Domain.Observer.DisplayBoardUpdater(dbContext));
                
                subject.Status = update.NewStatus; // This will call Notify() and write AuditLogs
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

        private int _passengerSyncTickCount = 0;

        private async Task SyncPassengerFlowAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            List<SimulatedFlight> flightsToSync;
            lock (_flightLock)
            {
                // Sync flights that haven't departed yet
                flightsToSync = _activeFlights.Where(f => VirtualTime < f.DepartureTime).ToList();
            }

            foreach (var f in flightsToSync)
            {
                var timeToDeparture = (f.DepartureTime - VirtualTime).TotalMinutes;

                // --- Dynamic windows matching TickAsync proportional thresholds ---
                double fMin = f.TotalTimeHours * 60.0;
                double checkInOpenMin  = Math.Min(120.0, fMin * 2.0);   // check-in opens min(2h, 2× flight dur)
                double boardingStartMin = Math.Clamp(fMin * 0.25, 20.0, 90.0); // boarding min(20 min, 25% dur)

                var tickets = await dbContext.Tickets.Where(t => t.FlightId == f.Id).ToListAsync();
                var bags = await dbContext.BaggageItems.Where(b => b.FlightId == f.Id).ToListAsync();

                bool dbChanged = false;

                if (timeToDeparture <= checkInOpenMin && timeToDeparture > boardingStartMin) // Check-in phase
                {
                    var issued = tickets.Where(t => t.TicketState == "Issued" && t.UserId == null).ToList();
                    int numToCheckIn = Math.Max(1, issued.Count / 10);
                    foreach (var t in issued.OrderBy(x => _rng.Next()).Take(numToCheckIn))
                    {
                        t.TicketState = "CheckedIn";
                        t.CheckInAt = VirtualTime;
                        dbChanged = true;
                    }
                    
                    var pendingBags = bags.Where(b => b.BaggageStage == "PendingCheckIn").ToList();
                    int bagsToCheckIn = Math.Max(1, pendingBags.Count / 10);
                    foreach (var b in pendingBags.OrderBy(x => _rng.Next()).Take(bagsToCheckIn))
                    {
                        b.BaggageStage = "CheckedIn"; // Properly move to CheckedIn instead of OnConveyor
                        b.StageUpdatedAt = VirtualTime;
                        dbChanged = true;
                    }
                }
                
                // Baggage sorting logic has been delegated entirely to AdvanceBaggageStagesAsync
                // to prevent skipping the XRayScreening stage.

                if (timeToDeparture <= boardingStartMin && timeToDeparture > 0) // Boarding phase
                {
                    var checkedIn = tickets.Where(t => t.TicketState == "CheckedIn" && t.UserId == null).ToList();
                    int numToBoard = Math.Max(1, checkedIn.Count / 5);
                    foreach (var t in checkedIn.OrderBy(x => _rng.Next()).Take(numToBoard))
                    {
                        t.TicketState = "Boarded";
                        t.BoardedAt = VirtualTime;
                        dbChanged = true;
                    }
                }

                if (dbChanged)
                {
                    await dbContext.SaveChangesAsync();
                    
                    lock (_flightLock)
                    {
                        var flight = _activeFlights.FirstOrDefault(x => x.FlightNumber == f.FlightNumber);
                        if (flight != null)
                        {
                            flight.CheckedInCount = tickets.Count(t => t.TicketState != "Issued" && t.TicketState != "Cancelled");
                            flight.BoardedCount = tickets.Count(t => t.TicketState == "Boarded");
                            flight.BagsLoadedCount = bags.Count(b => b.BaggageStage == "LoadedOnAircraft");
                            flight.TotalPassengers = tickets.Count;
                        }
                    }
                }
            }
        }

        private async Task AdvanceBaggageStagesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var stages = new[] { "CheckedIn", "OnConveyor", "XRayScreening", "Sorted", "LoadedOnAircraft" };
            var bags = await dbContext.BaggageItems.Where(b => b.BaggageStage != "LoadedOnAircraft" && b.SecurityStatus != "Rejected").ToListAsync();
            var now = VirtualTime; // Use simulation's VirtualTime instead of real-world DateTime.Now!
            bool changed = false;

            foreach (var bag in bags)
            {
                if (!bag.StageUpdatedAt.HasValue || (now - bag.StageUpdatedAt.Value).TotalSeconds >= 5)
                {
                    var idx = Array.IndexOf(stages, bag.BaggageStage);
                    if (idx >= 0 && idx < stages.Length - 1)
                    {
                        bag.BaggageStage = stages[idx + 1];
                        bag.StageUpdatedAt = now;
                        // Hold it here until Security Clears it
                        if (bag.BaggageStage == "XRayScreening" && (bag.SecurityStatus == "Flagged" || bag.SecurityStatus == "Pending"))
                            bag.BaggageStage = "XRayScreening"; 
                        changed = true;
                    }
                }
            }
            if (changed) await dbContext.SaveChangesAsync();
        }

        private int _lifecycleSyncTickCount = 0;

        private async Task ManageAirportLifecycleAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            int fleetSize = await dbContext.Aircrafts.CountAsync();

            // 1. Garbage Collection
            List<SimulatedFlight> completedFlights;
            lock (_flightLock)
            {
                completedFlights = _activeFlights.Where(f => f.Status == "Completed").ToList();
                foreach (var f in completedFlights)
                {
                    _activeFlights.Remove(f);
                }

                // HARD CAP: each aircraft can have at most 2 active flights at once
                // (current flight + 1 upcoming scheduled). So max = fleetSize * 2
                int hardCap = fleetSize * 2;
                var toRemoveList = _activeFlights
                    .Where(f => f.Status == "Landed")
                    .OrderBy(f => f.DepartureTime)
                    .ToList();

                int idx = 0;
                while (_activeFlights.Count > hardCap && idx < toRemoveList.Count)
                {
                    var f = toRemoveList[idx++];
                    _activeFlights.Remove(f);
                    completedFlights.Add(f); // Mark it as completed in DB!
                    _logger.LogWarning($"Hard cap enforced: removed excess landed flight {f.FlightNumber} from RAM and marked as Completed.");
                }
            }

            foreach (var f in completedFlights)
            {
                var dbFlight = await dbContext.Flights.FindAsync(f.Id);
                if (dbFlight != null)
                {
                    dbFlight.Status = "Completed";
                }
            }

            if (completedFlights.Any())
            {
                await dbContext.SaveChangesAsync();
            }

            // 2. Continuous Generator (Strictly Fleet-Aware)
            var aircraftList = await dbContext.Aircrafts.ToListAsync();
            if (!aircraftList.Any()) return;

            var newFlights = new List<TMPP_Aeroport.Models.Flight>();

            foreach (var ac in aircraftList)
            {
                // Find the latest flight for this aircraft that is not completed
                var latestFlight = await dbContext.Flights
                    .Where(f => f.AircraftId == ac.Id && f.Status != "Completed" && f.Status != "Cancelled")
                    .OrderByDescending(f => f.DepartureTime)
                    .FirstOrDefaultAsync();

                DateTime nextAvailableTime = VirtualTime;

                if (latestFlight != null)
                {
                    if (latestFlight.ArrivalTime.HasValue)
                        nextAvailableTime = latestFlight.ArrivalTime.Value.AddHours(2); // 2 hours turnaround
                    else
                        nextAvailableTime = latestFlight.DepartureTime.AddHours(4); // approx 3h flight + 1h turnaround
                }

                // If this aircraft has no upcoming flights in the next 12 hours, schedule one
                if (nextAvailableTime < VirtualTime.AddHours(12))
                {
                    var depTime = nextAvailableTime < VirtualTime 
                        ? VirtualTime.AddHours(_rng.Next(1, 3))
                        : nextAvailableTime.AddHours(_rng.Next(1, 3));

                    var origin = Airports.Keys.ElementAt(_rng.Next(Airports.Count));
                    var dest = Airports.Keys.ElementAt(_rng.Next(Airports.Count));
                    
                    // Logic: the new origin should ideally be the old destination
                    if (latestFlight != null && !string.IsNullOrEmpty(latestFlight.Destination))
                    {
                        origin = latestFlight.Destination;
                        if (!Airports.ContainsKey(origin)) origin = Airports.Keys.ElementAt(_rng.Next(Airports.Count));
                    }
                    
                    while (origin == dest) dest = Airports.Keys.ElementAt(_rng.Next(Airports.Count));

                    var flt = new TMPP_Aeroport.Models.Flight
                    {
                        FlightNumber = $"RO{_rng.Next(100, 9999)}",
                        Origin = origin,
                        Destination = dest,
                        DepartureTime = depTime,
                        Status = "Scheduled",
                        MaxCapacity = ac.Capacity,
                        AircraftId = ac.Id
                    };
                    newFlights.Add(flt);
                }
            }

            if (newFlights.Any())
            {
                await dbContext.Flights.AddRangeAsync(newFlights);
                await dbContext.SaveChangesAsync();

                // Load newly generated flights into simulation (which also generates tickets)
                foreach(var f in newFlights)
                {
                    await LoadFlightIntoSimulationAsync(f, dbContext);
                }
            }
        }

        // SaveState removed: simulation state is now derived purely from the database on startup.
        // This prevents ghost flight accumulation across restarts.
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

        // Feature 4: Interactive Boarding & Passenger Flow
        public int TotalPassengers { get; set; } = 0; // Legacy / Fallback
        public int BoardedCount { get; set; } = 0;
        public bool BoardingStarted { get; set; } = false;
        
        // Automated Passenger Flow Tracking
        public bool PassengersGenerated { get; set; } = false;
        public int TotalTickets { get; set; } = 0;
        public int CheckedInCount { get; set; } = 0;
        public int TotalBags { get; set; } = 0;
        public int BagsLoadedCount { get; set; } = 0;
    }
}
