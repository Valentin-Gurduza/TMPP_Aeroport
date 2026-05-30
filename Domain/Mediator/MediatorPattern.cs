using System;
using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Mediator
{
    // 1. Mediator Interface
    public interface IATCMediator
    {
        void RegisterAircraft(Aircraft aircraft);
        void SendMessage(string message, Aircraft sender);
        void RequestLanding(Aircraft aircraft);
    }

    // 2. Concrete Mediator
    public class ATCTower : IATCMediator
    {
        private List<Aircraft> _aircrafts = new List<Aircraft>();
        public List<string> ATCLogs { get; } = new List<string>();
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

        public ATCTower(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        private void SaveMessageToDb(string senderFlight, string content, string msgType, bool fromTower)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TMPP_Aeroport.Data.ApplicationDbContext>();
            db.AirportMessages.Add(new TMPP_Aeroport.Models.AirportMessage
            {
                SenderFlight = senderFlight,
                Content = content,
                MessageType = msgType,
                IsFromTower = fromTower
            });
            db.SaveChanges();
        }

        public void RegisterAircraft(Aircraft aircraft)
        {
            if (!_aircrafts.Contains(aircraft))
            {
                _aircrafts.Add(aircraft);
                string log = $"[ATC TOWER] {aircraft.CallSign} registered in airspace.";
                ATCLogs.Add(log);
            }
        }

        public void SendMessage(string message, Aircraft sender)
        {
            SaveMessageToDb(sender.CallSign, message, "Info", false);

            foreach (var aircraft in _aircrafts)
            {
                if (aircraft != sender)
                {
                    aircraft.ReceiveMessage(message, sender.CallSign);
                }
            }
        }

        public void RequestLanding(Aircraft aircraft)
        {
            string log = $"[ATC TOWER] {aircraft.CallSign} requesting landing.";
            ATCLogs.Add(log);
            SaveMessageToDb(aircraft.CallSign, "Requesting landing clearance.", "Warning", false);

            string response = $"Please hold position, {aircraft.CallSign} is landing.";
            SendMessage(response, aircraft);
            SaveMessageToDb("TOWER", response, "Info", true);
            
            ATCLogs.Add($"[ATC TOWER] Clearance granted to {aircraft.CallSign}.");
        }
    }

    // 3. Colleague (Base Class)
    public abstract class Aircraft
    {
        protected IATCMediator _mediator;
        public string CallSign { get; }
        public List<string> AircraftLogs { get; } = new List<string>();

        public Aircraft(IATCMediator mediator, string callSign)
        {
            _mediator = mediator;
            CallSign = callSign;
            _mediator.RegisterAircraft(this);
        }

        public void Send(string message)
        {
            AircraftLogs.Add($"[{CallSign}] Sending: {message}");
            _mediator.SendMessage(message, this);
        }

        public void RequestLanding()
        {
            AircraftLogs.Add($"[{CallSign}] Requesting landing clearance.");
            _mediator.RequestLanding(this);
        }

        public virtual void ReceiveMessage(string message, string senderCallSign)
        {
            AircraftLogs.Add($"[{CallSign}] Received from {senderCallSign}: {message}");
        }
    }

    // 4. Concrete Colleagues
    public class CommercialFlight : Aircraft
    {
        public CommercialFlight(IATCMediator mediator, string callSign) : base(mediator, callSign) { }

        public override void ReceiveMessage(string message, string senderCallSign)
        {
            AircraftLogs.Add($"[COMMERCIAL {CallSign}] Copy that {senderCallSign}. Message: {message}");
        }
    }

    public class Helicopter : Aircraft
    {
        public Helicopter(IATCMediator mediator, string callSign) : base(mediator, callSign) { }

        public override void ReceiveMessage(string message, string senderCallSign)
        {
            AircraftLogs.Add($"[HELI {CallSign}] Understood {senderCallSign}: {message}");
        }
    }
}
