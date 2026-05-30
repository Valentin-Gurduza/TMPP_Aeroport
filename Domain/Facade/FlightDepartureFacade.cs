using System;
using System.Collections.Generic;
using TMPP_Aeroport.Domain.Singleton;

namespace TMPP_Aeroport.Domain.Facade
{
    // =========================================================
    // SUBSYSTEM 1: Air Traffic Control (Greoi, necesită aprobări)
    // =========================================================
    public class ATCService
    {
        public bool RequestTakeoffClearance(string flightNumber, string runway)
        {
            AirportLogger.Instance.Log($"[ATC] Zborul {flightNumber} cere permisiunea pe pista {runway}...");
            // În realitate, ar fi comunicare de rețea complicată.
            return true; 
        }

        public void AssignDepartureRoute(string flightNumber)
        {
            AirportLogger.Instance.Log($"[ATC] Ruta de plecare asignată pentru {flightNumber}.");
        }
    }

    // =========================================================
    // SUBSYSTEM 2: Crew Management 
    // =========================================================
    public class CrewManagementService
    {
        public bool IsCrewReady(string flightNumber)
        {
            AirportLogger.Instance.Log($"[Crew] Verificare prezență piloți și însoțitori pentru {flightNumber}...");
            return true;
        }

        public void CloseCabinDoors()
        {
            AirportLogger.Instance.Log($"[Crew] Ușile aeronavei au fost încuiate și armate.");
        }
    }

    // =========================================================
    // SUBSYSTEM 3: Baggage Loading 
    // =========================================================
    public class BaggageLoadingService
    {
        public bool IsCargoLoaded(string flightNumber)
        {
            AirportLogger.Instance.Log($"[Baggage] Calarea tuturor containerelor pentru {flightNumber} confirmată.");
            return true;
        }
    }

    // FACADE: Interfața Unificată
    // Ascunde toți pașii complecși și obiectele individuale 
    // de mai sus într-un singur buton logic.
    public class FlightDepartureFacade
    {
        private readonly ATCService _atc;
        private readonly CrewManagementService _crew;
        private readonly BaggageLoadingService _baggage;
        private readonly TMPP_Aeroport.Data.ApplicationDbContext _dbContext;

        public FlightDepartureFacade(TMPP_Aeroport.Data.ApplicationDbContext dbContext)
        {
            _atc = new ATCService();
            _crew = new CrewManagementService();
            _baggage = new BaggageLoadingService();
            _dbContext = dbContext;
        }

        public (bool Success, List<string> Logs) AuthoriseDeparture(string flightNumber, string runway)
        {
            List<string> stepResults = new List<string>();
            stepResults.Add($"Începere secvență decolare pentru zbor {flightNumber} pe pista {runway}...");

            if (!_baggage.IsCargoLoaded(flightNumber))
            {
                stepResults.Add("EROARE: Bagajele nu sunt gata!");
                return (false, stepResults);
            }
            stepResults.Add("- Cargo confirmat complet.");

            if (!_crew.IsCrewReady(flightNumber))
            {
                stepResults.Add("EROARE: Echipaj incomplet!");
                return (false, stepResults);
            }
            stepResults.Add("- Echipaj pregătit.");

            _crew.CloseCabinDoors();
            stepResults.Add("- Uși închise și securizate.");

            _atc.AssignDepartureRoute(flightNumber);
            stepResults.Add("- Ruta oficială primită de la Turn.");

            if (_atc.RequestTakeoffClearance(flightNumber, runway))
            {
                stepResults.Add($"[SUCCES] Avionul {flightNumber} are aprobare finală (Clearance). Zbor pregătit de decolare!");
            }

            // Update Database
            var flight = _dbContext.Flights.FirstOrDefault(f => f.FlightNumber == flightNumber);
            if (flight != null)
            {
                flight.Status = TMPP_Aeroport.Models.FlightStatus.ReadyForDeparture;
                _dbContext.SaveChanges();
                AirportLogger.Instance.Log($"[Facade] Zborul {flightNumber} a fost marcat ca ReadyForDeparture.");
            }

            return (true, stepResults);
        }
    }
}
