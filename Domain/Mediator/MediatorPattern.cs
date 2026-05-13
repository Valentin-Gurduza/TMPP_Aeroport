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

        public void RegisterAircraft(Aircraft aircraft)
        {
            if (!_aircrafts.Contains(aircraft))
            {
                _aircrafts.Add(aircraft);
                ATCLogs.Add($"[ATC TOWER] {aircraft.CallSign} registered in airspace.");
            }
        }

        public void SendMessage(string message, Aircraft sender)
        {
            foreach (var aircraft in _aircrafts)
            {
                // Don't send the message to the sender
                if (aircraft != sender)
                {
                    aircraft.ReceiveMessage(message, sender.CallSign);
                }
            }
        }

        public void RequestLanding(Aircraft aircraft)
        {
            ATCLogs.Add($"[ATC TOWER] {aircraft.CallSign} requesting landing.");
            // Simplify logic: allow landing if it's the first one, just for demo
            SendMessage($"Please hold position, {aircraft.CallSign} is landing.", aircraft);
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
