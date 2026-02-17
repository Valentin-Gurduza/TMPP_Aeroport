using System;
using System.Collections.Generic;
using TMPP_Aeroport.Domain.Entities;
using TMPP_Aeroport.Domain.Interfaces;

namespace TMPP_Aeroport.Services
{
    public class FlightService : IFlightService
    {
        private readonly List<Flight> _flights = new List<Flight>();

        public FlightService()
        {
            // Populăm cu date de test
            _flights.Add(new Flight("RO301", "Bucharest", "Frankfurt") { DepartureTime = DateTime.Now.AddHours(2), ArrivalTime = DateTime.Now.AddHours(5) });
            _flights.Add(new Flight("RO305", "Bucharest", "London") { DepartureTime = DateTime.Now.AddHours(4), ArrivalTime = DateTime.Now.AddHours(8) });
        }
// ...
        public AircraftService()
        {
            // Populăm cu date de test folosind tipuri diferite (Polimorfism)
            _aircrafts.Add(new PassengerPlane("Boeing 737", "YR-BGS", 180));
            _aircrafts.Add(new CargoPlane("Airbus A330F", "YR-CGO", 70000));
        }

        public IEnumerable<Aircraft> GetAllAircraft()
        {
            return _aircrafts;
        }

        public void RegisterAircraft(Aircraft aircraft)
        {
            _aircrafts.Add(aircraft);
        }
    }
}
