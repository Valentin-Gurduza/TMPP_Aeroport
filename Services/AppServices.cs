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
            
            // Singleton: Jurnalizăm instanțierea cu un singur obiect global
            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log("FlightService creat și populat cu zboruri de bază.");
        }

        public IEnumerable<Flight> GetAllFlights()
        {
            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log("A fost preluată lista cu toate zborurile.");
            return _flights;
        }

        public void ScheduleFlight(Flight flight)
        {
            _flights.Add(flight);
            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log($"Zbor nou programat: {flight.FlightNumber}");
        }

        // Prototype Pattern: Clonarea unui zbor existent
        public Flight CloneFlight(Guid flightId)
        {
            var original = _flights.Find(f => f.Id == flightId);
            if (original == null) return null;

            // Apelăm metoda de clonare care face Shallow Copy și mută zborul pe ziua următoare
            var clone = original.CloneForNextDay();
            _flights.Add(clone);

            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log($"Zborul {original.FlightNumber} a fost CLONAT (Prototype) pentru data {clone.DepartureTime:dd-MM-yyyy}");
            
            return clone;
        }

    }

    public class AircraftService : IAircraftService
    {
        private readonly List<Aircraft> _aircrafts = new List<Aircraft>();

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
