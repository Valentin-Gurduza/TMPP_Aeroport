using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
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
        private readonly TMPP_Aeroport.Data.ApplicationDbContext _context;

        public PassengerNotifier(TMPP_Aeroport.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public void Update(string flightNumber, string status)
        {
            // Decorator usage: wrap WebAppNotifier with SMS and Email
            TMPP_Aeroport.Domain.Decorator.IBoardingNotifier notifier = new TMPP_Aeroport.Domain.Decorator.WebAppNotifier();
            notifier = new TMPP_Aeroport.Domain.Decorator.SMSNotifierDecorator(notifier);
            notifier = new TMPP_Aeroport.Domain.Decorator.EmailNotifierDecorator(notifier);

            var logs = notifier.SendNotification("All Passengers", $"Flight {flightNumber} is now: {status}");

            foreach (var log in logs)
            {
                _context.AuditLogs.Add(new TMPP_Aeroport.Models.AuditLog 
                { 
                    Message = log, 
                    Category = "Notification" 
                });
            }
        }
    }

    // 5. Observer Concret 2
    public class DisplayBoardUpdater : IObserver
    {
        private readonly TMPP_Aeroport.Data.ApplicationDbContext _context;

        public DisplayBoardUpdater(TMPP_Aeroport.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public void Update(string flightNumber, string status)
        {
            _context.AuditLogs.Add(new TMPP_Aeroport.Models.AuditLog 
            { 
                Message = $"[Board Update] Updating terminal displays for {flightNumber} to {status}", 
                Category = "System" 
            });
        }
    }

    // --- Security Alerts Extension ---

    public interface ISecurityObserver
    {
        void Update(string checkpointId, string alertLevel, string message);
    }

    public class SecurityAlertSubject
    {
        private List<ISecurityObserver> _observers = new List<ISecurityObserver>();
        private string _checkpointId;
        private string _alertLevel;
        private string _message = string.Empty;

        public SecurityAlertSubject(string checkpointId)
        {
            _checkpointId = checkpointId;
            _alertLevel = "Normal";
        }

        public void Attach(ISecurityObserver observer)
        {
            _observers.Add(observer);
        }

        public void Detach(ISecurityObserver observer)
        {
            _observers.Remove(observer);
        }

        public void TriggerAlert(string level, string message)
        {
            _alertLevel = level;
            _message = message;
            Notify();
        }

        private void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(_checkpointId, _alertLevel, _message);
            }
        }
    }

    public class SignalRSecurityNotifier : ISecurityObserver
    {
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<TMPP_Aeroport.Hubs.FlightHub> _hubContext;

        public SignalRSecurityNotifier(Microsoft.AspNetCore.SignalR.IHubContext<TMPP_Aeroport.Hubs.FlightHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void Update(string checkpointId, string alertLevel, string message)
        {
            // Send broadcast via SignalR
            // Not awaiting since Observer Update is synchronous. We use Task.Run or _hubContext.Clients.All.SendAsync returns a Task.
            _ = _hubContext.Clients.All.SendAsync("SecurityAlert", new { CheckpointId = checkpointId, Level = alertLevel, Message = message });
        }
    }

    public class DatabaseSecurityLogger : ISecurityObserver
    {
        private readonly TMPP_Aeroport.Data.ApplicationDbContext _context;

        public DatabaseSecurityLogger(TMPP_Aeroport.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public void Update(string checkpointId, string alertLevel, string message)
        {
            _context.AuditLogs.Add(new TMPP_Aeroport.Models.AuditLog 
            { 
                Message = $"[SECURITY - {alertLevel}] Checkpoint {checkpointId}: {message}", 
                Category = "Security" 
            });
            _context.SaveChanges();
        }
    }
}
