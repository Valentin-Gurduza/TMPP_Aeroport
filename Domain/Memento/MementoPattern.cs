using System;
using System.Collections.Generic;
//Fara distruge incapsularea captureaza si externalizeaza starea interna a unui obiect
namespace TMPP_Aeroport.Domain.Memento
{
    // 1. Memento (stochează starea)
    public class FlightConfigMemento
    {
        public string Gate { get; }
        public DateTime DepartureTime { get; }
        public string AircraftModel { get; }

        public FlightConfigMemento(string gate, DateTime departureTime, string aircraftModel)
        {
            Gate = gate;
            DepartureTime = departureTime;
            AircraftModel = aircraftModel;
        }
    }

    // 2. Originator (obiectul a cărui stare o salvăm)
    public class FlightConfigurator
    {
        public string Gate { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string AircraftModel { get; set; } = string.Empty;
        public List<string> ActionLogs { get; } = new List<string>();

        public void SetConfiguration(string gate, DateTime departureTime, string aircraftModel)
        {
            Gate = gate;
            DepartureTime = departureTime;
            AircraftModel = aircraftModel;
            ActionLogs.Add($"Config updated: {Gate}, {DepartureTime.ToShortTimeString()}, {AircraftModel}");
        }

        // Crează memento-ul (salvează starea curentă)
        public FlightConfigMemento SaveToMemento()
        {
            ActionLogs.Add($"State saved to Memento.");
            return new FlightConfigMemento(Gate, DepartureTime, AircraftModel);
        }

        // Restaurează starea dintr-un memento
        public void RestoreFromMemento(FlightConfigMemento memento)
        {
            Gate = memento.Gate;
            DepartureTime = memento.DepartureTime;
            AircraftModel = memento.AircraftModel;
            ActionLogs.Add($"State restored from Memento: {Gate}, {DepartureTime.ToShortTimeString()}, {AircraftModel}");
        }
    }

    // 3. Caretaker (gestionează istoria memento-urilor)
    public class FlightConfigHistory
    {
        private Stack<FlightConfigMemento> _history = new Stack<FlightConfigMemento>();

        public void Backup(FlightConfigurator originator)
        {
            _history.Push(originator.SaveToMemento());
        }

        public void Undo(FlightConfigurator originator)
        {
            if (_history.Count == 0) return;

            var memento = _history.Pop();
            originator.RestoreFromMemento(memento);
        }
    }
}
