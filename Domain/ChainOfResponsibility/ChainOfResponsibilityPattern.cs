using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.ChainOfResponsibility
{
    public class Baggage
    {
        public string Owner { get; set; } = string.Empty;
        public double Weight { get; set; }
        public bool HasSuspiciousItems { get; set; }
        public bool HasExplosiveTraces { get; set; }
        public List<string> CheckLogs { get; } = new List<string>();
    }

    // 1. Handler Interface
    public interface IBaggageHandler
    {
        IBaggageHandler SetNext(IBaggageHandler handler);
        bool Handle(Baggage baggage);
    }

    // 2. Base Handler
    public abstract class BaseBaggageHandler : IBaggageHandler
    {
        private IBaggageHandler? _nextHandler;

        public IBaggageHandler SetNext(IBaggageHandler handler)
        {
            _nextHandler = handler;
            // Returning the handler allows chaining: handler1.SetNext(handler2).SetNext(handler3)
            return handler;
        }

        public virtual bool Handle(Baggage baggage)
        {
            if (_nextHandler != null)
            {
                return _nextHandler.Handle(baggage);
            }
            return true; // Reached the end of the chain successfully
        }
    }

    // 3. Concrete Handlers
    public class WeightCheckHandler : BaseBaggageHandler
    {
        public override bool Handle(Baggage baggage)
        {
            if (baggage.Weight > 32.0)
            {
                baggage.CheckLogs.Add($"❌ Weight check failed: {baggage.Weight}kg exceeds 32kg limit.");
                return false;
            }
            
            baggage.CheckLogs.Add($"✅ Weight check passed ({baggage.Weight}kg).");
            return base.Handle(baggage); // Pass to next
        }
    }

    public class XRayScanHandler : BaseBaggageHandler
    {
        public override bool Handle(Baggage baggage)
        {
            if (baggage.HasSuspiciousItems)
            {
                baggage.CheckLogs.Add($"❌ X-Ray scan failed: Suspicious items detected in {baggage.Owner}'s baggage.");
                return false;
            }

            baggage.CheckLogs.Add($"✅ X-Ray scan passed. No suspicious items.");
            return base.Handle(baggage);
        }
    }

    public class ExplosiveTraceHandler : BaseBaggageHandler
    {
        public override bool Handle(Baggage baggage)
        {
            if (baggage.HasExplosiveTraces)
            {
                baggage.CheckLogs.Add($"🚨 ALARM: Explosive traces detected in {baggage.Owner}'s baggage! Security alerted.");
                return false;
            }

            baggage.CheckLogs.Add($"✅ Explosive trace check passed. Baggage cleared for loading.");
            return base.Handle(baggage);
        }
    }
}
