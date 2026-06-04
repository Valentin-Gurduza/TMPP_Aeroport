using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TMPP_Aeroport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TMPP_Aeroport.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "ATC_Manager", "Ground_Staff", "Passenger" };

            // Seed Roles
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            var adminUser = await userManager.FindByEmailAsync("admin@sams.local");
            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "admin@sams.local",
                    Email = "admin@sams.local",
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };

                var config = serviceProvider.GetRequiredService<IConfiguration>();
                string? adminPassword = config["SeedAdminPassword"];
                if (string.IsNullOrEmpty(adminPassword))
                {
                    throw new InvalidOperationException("SeedAdminPassword must be configured in appsettings or environment variables.");
                }

                var createPowerUser = await userManager.CreateAsync(newAdmin, adminPassword);
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            // Seed Aircrafts and Flights
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Only seed if empty (do not clear existing data)
            
            if (!context.Aircrafts.Any())
            {
                var aircrafts = new List<Aircraft>
                {
                    new Aircraft { RegistrationCode = "YR-BGG", Model = "Boeing 737-800", Capacity = 189, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "YR-BGH", Model = "Boeing 737-800", Capacity = 189, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "YR-BGI", Model = "Boeing 737-700", Capacity = 148, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "F-GKXN", Model = "Airbus A320", Capacity = 174, Airline = "Air France" },
                    new Aircraft { RegistrationCode = "F-GKXO", Model = "Airbus A321", Capacity = 212, Airline = "Air France" },
                    new Aircraft { RegistrationCode = "PH-BXO", Model = "Boeing 737-800", Capacity = 186, Airline = "KLM" },
                    new Aircraft { RegistrationCode = "PH-BXP", Model = "Boeing 737-900", Capacity = 204, Airline = "KLM" },
                    new Aircraft { RegistrationCode = "G-EUUI", Model = "Airbus A320", Capacity = 180, Airline = "British Airways" },
                    new Aircraft { RegistrationCode = "D-AIZE", Model = "Airbus A320", Capacity = 168, Airline = "Lufthansa" },
                    new Aircraft { RegistrationCode = "EC-MKL", Model = "Airbus A320", Capacity = 180, Airline = "Iberia" },
                    new Aircraft { RegistrationCode = "EI-DCL", Model = "Boeing 737-800", Capacity = 189, Airline = "Ryanair" },
                    new Aircraft { RegistrationCode = "HA-LPM", Model = "Airbus A320", Capacity = 180, Airline = "Wizz Air" }
                };
                context.Aircrafts.AddRange(aircrafts);
                await context.SaveChangesAsync();
            }

            if (!context.Flights.Any())
            {
                var aircraftList = context.Aircrafts.ToList();
                if (aircraftList.Count > 0)
                {
                    var now = DateTime.Now;
                    var flights = new List<Flight>
                    {
                        // Airborne right now (Departed 1 hour ago)
                        new Flight { FlightNumber = "RO301", Destination = "Frankfurt FRA", DepartureTime = now.AddHours(-1), ArrivalTime = now.AddHours(1), Status = TMPP_Aeroport.Models.FlightStatus.Airborne, AircraftId = aircraftList[0].Id, Terminal = "T1", Gate = "A1", MaxCapacity = 189, BaggageLimitKg = 4000 },
                        new Flight { FlightNumber = "AF108", Destination = "Bucharest OTP", DepartureTime = now.AddHours(-1.5), ArrivalTime = now.AddHours(0.5), Status = TMPP_Aeroport.Models.FlightStatus.Airborne, AircraftId = aircraftList[3].Id, Terminal = "T2", Gate = "B2", MaxCapacity = 174, BaggageLimitKg = 3500 },
                        new Flight { FlightNumber = "KLM99", Destination = "Amsterdam AMS", DepartureTime = now.AddMinutes(-45), ArrivalTime = now.AddMinutes(90), Status = TMPP_Aeroport.Models.FlightStatus.Airborne, AircraftId = aircraftList[5].Id, Terminal = "T1", Gate = "A3", MaxCapacity = 186, BaggageLimitKg = 3800 },
                        new Flight { FlightNumber = "BA402", Destination = "Rome FCO", DepartureTime = now.AddMinutes(-120), ArrivalTime = now.AddMinutes(30), Status = TMPP_Aeroport.Models.FlightStatus.Airborne, AircraftId = aircraftList[7].Id, Terminal = "T3", Gate = "C1", MaxCapacity = 180, BaggageLimitKg = 3600 },
                        
                        // Boarding right now (Departing in 15 mins)
                        new Flight { FlightNumber = "RO381", Destination = "Paris CDG", DepartureTime = now.AddMinutes(15), ArrivalTime = now.AddHours(3), Status = TMPP_Aeroport.Models.FlightStatus.Boarding, AircraftId = aircraftList[1].Id, Terminal = "T1", Gate = "A2", MaxCapacity = 189, BaggageLimitKg = 4000 },
                        new Flight { FlightNumber = "LH202", Destination = "Berlin BER", DepartureTime = now.AddMinutes(5), ArrivalTime = now.AddHours(2.5), Status = TMPP_Aeroport.Models.FlightStatus.Boarding, AircraftId = aircraftList[8].Id, Terminal = "T2", Gate = "B1", MaxCapacity = 168, BaggageLimitKg = 3300 },
                        new Flight { FlightNumber = "W6314", Destination = "London LHR", DepartureTime = now.AddMinutes(25), ArrivalTime = now.AddHours(3.5), Status = TMPP_Aeroport.Models.FlightStatus.Boarding, AircraftId = aircraftList[11].Id, Terminal = "T3", Gate = "C3", MaxCapacity = 180, BaggageLimitKg = 3600 },

                        // Scheduled soon
                        new Flight { FlightNumber = "RO361", Destination = "Amsterdam AMS", DepartureTime = now.AddMinutes(45), ArrivalTime = now.AddHours(3.5), Status = TMPP_Aeroport.Models.FlightStatus.Scheduled, AircraftId = aircraftList[2].Id, Terminal = "T1", Gate = "A4", MaxCapacity = 148, BaggageLimitKg = 3000 },
                        new Flight { FlightNumber = "IB311", Destination = "Madrid MAD", DepartureTime = now.AddMinutes(90), ArrivalTime = now.AddHours(4.5), Status = TMPP_Aeroport.Models.FlightStatus.Scheduled, AircraftId = aircraftList[9].Id, Terminal = "T2", Gate = "B3", MaxCapacity = 180, BaggageLimitKg = 3600 },
                        new Flight { FlightNumber = "FR102", Destination = "Rome FCO", DepartureTime = now.AddHours(2), ArrivalTime = now.AddHours(4), Status = TMPP_Aeroport.Models.FlightStatus.Scheduled, AircraftId = aircraftList[10].Id, Terminal = "T3", Gate = "C2", MaxCapacity = 189, BaggageLimitKg = 4000 },
                        
                        // Scheduled future (Tomorrow)
                        new Flight { FlightNumber = "RO315", Destination = "Munich MUC", DepartureTime = now.AddDays(1).AddHours(2), ArrivalTime = now.AddDays(1).AddHours(4.5), Status = TMPP_Aeroport.Models.FlightStatus.Scheduled, AircraftId = aircraftList[0].Id, Terminal = "T1", Gate = "A1", MaxCapacity = 189, BaggageLimitKg = 4000 },
                        new Flight { FlightNumber = "RO391", Destination = "London LHR", DepartureTime = now.AddDays(1).AddHours(6), ArrivalTime = now.AddDays(1).AddHours(9.5), Status = TMPP_Aeroport.Models.FlightStatus.Scheduled, AircraftId = aircraftList[1].Id, Terminal = "T1", Gate = "A2", MaxCapacity = 189, BaggageLimitKg = 4000 },
                        new Flight { FlightNumber = "AF202", Destination = "Paris CDG", DepartureTime = now.AddDays(2).AddHours(1), ArrivalTime = now.AddDays(2).AddHours(4), Status = TMPP_Aeroport.Models.FlightStatus.Scheduled, AircraftId = aircraftList[4].Id, Terminal = "T2", Gate = "B2", MaxCapacity = 212, BaggageLimitKg = 4500 },
                        new Flight { FlightNumber = "KLM101", Destination = "Amsterdam AMS", DepartureTime = now.AddDays(3), ArrivalTime = now.AddDays(3).AddHours(3), Status = TMPP_Aeroport.Models.FlightStatus.Scheduled, AircraftId = aircraftList[6].Id, Terminal = "T1", Gate = "A3", MaxCapacity = 204, BaggageLimitKg = 4200 }
                    };
                    context.Flights.AddRange(flights);
                    await context.SaveChangesAsync();
                }

                // 4. Seed BaggageItems for Cargo Manifest
                if (!context.BaggageItems.Any())
                {
                    var activeFlights = context.Flights.Where(f => f.Status == TMPP_Aeroport.Models.FlightStatus.Scheduled || f.Status == TMPP_Aeroport.Models.FlightStatus.Boarding).ToList();
                    var random = new Random();
                    var baggageItems = new List<BaggageItem>();

                    foreach (var flight in activeFlights)
                    {
                        int numBaggage = random.Next(5, 15);
                        for (int i = 0; i < numBaggage; i++)
                        {
                            bool isBackpack = random.NextDouble() > 0.7;
                            baggageItems.Add(new BaggageItem
                            {
                                FlightId = flight.Id,
                                TagCode = flight.FlightNumber + "-" + random.Next(1000, 9999).ToString(),
                                Weight = isBackpack ? (int)Math.Round(random.NextDouble() * 10 + 2) : (int)Math.Round(random.NextDouble() * 15 + 10),
                                Type = isBackpack ? "Backpack" : "Suitcase"
                            });
                        }
                    }

                    await context.BaggageItems.AddRangeAsync(baggageItems);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
