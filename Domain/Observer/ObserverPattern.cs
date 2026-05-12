using System;
using System.Collections.Generic;
// Defineste o dependenta unu-la-multi
// Cand un obiect isi schimba starea sunt notify

namespace TMPP_Aeroport.Domain.Observer
{
    // 1. Interfața Observer
    public interface IObserver
    {
        void Update(string flightNumber, string status);
    }

    // 2. Interfața Subject
    public interface ISubject
    {
        void Attach(IObserver observer);
        void Detach(IObserver observer);
        void Notify();
    }

    // 3. Subject Concret
    public class FlightStatusSubject : ISubject
    {
        private List<IObserver> _observers = new List<IObserver>();
        private string _flightNumber;
        private string _status;

        public FlightStatusSubject(string flightNumber)
        {
            _flightNumber = flightNumber;
            _status = "Scheduled";
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                Notify(); // Notificăm toți observatorii când starea se schimbă
            }
        }

        public void Attach(IObserver observer)
        {
            _observers.Add(observer);
        }

        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }

        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(_flightNumber, _status);
            }
        }
    }

    // 4. Observer Concret 1
    public class PassengerNotifier : IObserver
    {
        private List<string> _logs;

        public PassengerNotifier(List<string> logs)
        {
            _logs = logs;
        }

        public void Update(string flightNumber, string status)
        {
            _logs.Add($"[Passenger SMS] Flight {flightNumber} is now: {status}");
        }
    }

    // 5. Observer Concret 2
    public class DisplayBoardUpdater : IObserver
    {
        private List<string> _logs;

        public DisplayBoardUpdater(List<string> logs)
        {
            _logs = logs;
        }

        public void Update(string flightNumber, string status)
        {
            _logs.Add($"[Board Update] Updating terminal displays for {flightNumber} to {status}");
        }
    }
}
