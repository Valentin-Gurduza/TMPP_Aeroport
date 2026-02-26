using System;
using System.Collections.Generic;
using TMPP_Aeroport.Domain.Entities;
using TMPP_Aeroport.Domain.Interfaces;
using TMPP_Aeroport.Domain.FactoryMethod;

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
            // Factory Method Pattern Usage
            // În loc să folosim 'new PassengerPlane(...)', folosim fabricile dedicate.
            // Acest lucru decuplează serviciul de crearea efectivă a obiectelor.

            AircraftFactory passengerFactory = new PassengerPlaneFactory();
            AircraftFactory cargoFactory = new CargoPlaneFactory();

            // Creăm un avion de pasageri cu capacitate 180
            _aircrafts.Add(passengerFactory.CreateAircraft("Boeing 737", "YR-BGS", 180));

            // Creăm un avion cargo cu greutate max 70000
            _aircrafts.Add(cargoFactory.CreateAircraft("Airbus A330F", "YR-CGO", 70000.0));
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
