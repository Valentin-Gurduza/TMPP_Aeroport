using Microsoft.AspNetCore.SignalR;
using TMPP_Aeroport.Hubs;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;
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

        public static int GlobalSpeedMultiplier = 1; // 1x by default
        private static readonly int TICK_INTERVAL_MS = 1000; // 1 second real-time tick
        
        public static DateTime VirtualTime { get; private set; }

        private List<SimulatedFlight> _activeFlights = new List<SimulatedFlight>();

        public FlightSimulationService(ILogger<FlightSimulationService> logger, IServiceScopeFactory scopeFactory, IHubContext<FlightHub> hubContext)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            VirtualTime = DateTime.Now; // Initialize Virtual Time to real time
        }

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

        private async Task InitializeFlightsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var dbFlights = await dbContext.Flights.Include(f => f.Aircraft).ToListAsync();

            // Airports mapping (from Radar)
            var airports = new Dictionary<string, (double Lat, double Lng)>
            {
                { "Bucharest", (44.5722, 26.1022) },
                { "Paris", (49.0097, 2.5479) },
                { "Frankfurt", (50.0379, 8.5622) },
                { "London", (51.4700, -0.4543) },
                { "Amsterdam", (52.3105, 4.7683) },
                { "Rome", (41.7999, 12.2462) },
                { "Madrid", (40.4983, -3.5676) },
                { "Berlin", (52.3667, 13.5033) }
            };

            foreach (var f in dbFlights)
            {
                // Assign a dummy origin just for simulation (OTP if destination is not OTP)
                var originName = f.Destination.Contains("Bucharest") ? "Paris" : "Bucharest";
                var destName = "Paris"; // Default fallback
                
                foreach (var apt in airports.Keys)
                {
                    if (f.Destination.Contains(apt)) destName = apt;
                }

                if (!airports.ContainsKey(originName)) originName = "Bucharest";
                if (!airports.ContainsKey(destName)) destName = "Paris";

                var origin = airports[originName];
                var dest = airports[destName];

                double distanceKm = CalculateDistance(origin.Lat, origin.Lng, dest.Lat, dest.Lng);
                double defaultSpeedKmh = 900.0;
                double totalTimeHours = distanceKm / defaultSpeedKmh;

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
                    Progress = 0
                };

                _activeFlights.Add(simFlight);
            }
        }

        private async Task TickAsync()
        {
            // Advance Virtual Time
            VirtualTime = VirtualTime.AddSeconds((TICK_INTERVAL_MS / 1000.0) * GlobalSpeedMultiplier);
            
            // Broadcast Time to UI
            await _hubContext.Clients.All.SendAsync("TimeUpdate", new { 
                VirtualTime = VirtualTime.ToString("dd MMM yyyy, HH:mm:ss"),
                Speed = GlobalSpeedMultiplier
            });

            bool anyStateChanged = false;

            foreach (var f in _activeFlights)
            {
                string oldStatus = f.Status;
                
                DateTime arrivalTime = f.DepartureTime.AddHours(f.TotalTimeHours);
                DateTime boardingTime = f.DepartureTime.AddMinutes(-30);
                DateTime deplaningEndTime = arrivalTime.AddMinutes(45);

                if (VirtualTime < boardingTime)
                {
                    f.Status = "Scheduled";
                    f.Progress = 0;
                    f.CurrentLat = f.OriginLat;
                    f.CurrentLng = f.OriginLng;
                }
                else if (VirtualTime >= boardingTime && VirtualTime < f.DepartureTime)
                {
                    f.Status = "Boarding";
                    f.Progress = 0;
                    f.CurrentLat = f.OriginLat;
                    f.CurrentLng = f.OriginLng;
                }
                else if (VirtualTime >= f.DepartureTime && VirtualTime < arrivalTime)
                {
                    f.Status = "Airborne";
                    // Calculate exact progress based on virtual time elapsed
                    TimeSpan elapsed = VirtualTime - f.DepartureTime;
                    double flightDurationSeconds = f.TotalTimeHours * 3600;
                    f.Progress = elapsed.TotalSeconds / flightDurationSeconds;
                    
                    if(f.Progress > 1.0) f.Progress = 1.0;

                    f.CurrentLat = f.OriginLat + (f.DestLat - f.OriginLat) * f.Progress;
                    f.CurrentLng = f.OriginLng + (f.DestLng - f.OriginLng) * f.Progress;
                }
                else if (VirtualTime >= arrivalTime && VirtualTime < deplaningEndTime)
                {
                    f.Status = "Landed";
                    f.Progress = 1.0;
                    f.CurrentLat = f.DestLat;
                    f.CurrentLng = f.DestLng;
                }
                else if (VirtualTime >= deplaningEndTime)
                {
                    // Reset cycle - simulate return flight exactly 2 hours after arrival
                    f.DepartureTime = deplaningEndTime.AddHours(1.25); // Gives 1.25h until next departure (45m landed + 1h15 wait)
                    
                    var tempLat = f.OriginLat;
                    var tempLng = f.OriginLng;
                    var tempName = f.OriginName;
                    
                    f.OriginLat = f.DestLat;
                    f.OriginLng = f.DestLng;
                    f.OriginName = f.DestName;
                    
                    f.DestLat = tempLat;
                    f.DestLng = tempLng;
                    f.DestName = tempName;
                    
                    f.Status = "Scheduled";
                    f.Progress = 0;
                    f.CurrentLat = f.OriginLat;
                    f.CurrentLng = f.OriginLng;
                }

                if (oldStatus != f.Status)
                {
                    anyStateChanged = true;
                    // Notify Global State Change
                    await _hubContext.Clients.All.SendAsync("FlightStateChanged", new {
                        FlightNumber = f.FlightNumber,
                        OldStatus = oldStatus,
                        NewStatus = f.Status,
                        Origin = f.OriginName,
                        Destination = f.DestName
                    });

                    // Update Database
                    await UpdateDatabaseStatusAsync(f.Id, f.Status);
                }
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

        private async Task UpdateDatabaseStatusAsync(int flightId, string newStatus)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var flight = await dbContext.Flights.FindAsync(flightId);
            if (flight != null)
            {
                flight.Status = newStatus;
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
    }
}
