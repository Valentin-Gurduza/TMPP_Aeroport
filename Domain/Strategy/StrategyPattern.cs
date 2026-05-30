namespace TMPP_Aeroport.Domain.Strategy
//Permite definirea diferitelor variante ale unui algoritm
//Si incapsularea fiecaruia

{
    // 1. Interfața comună pentru strategii
    public interface ITicketPricingStrategy
    {
        double CalculatePrice(double basePrice);
    }

    // 2. Strategii Concrete
    public class RegularPricingStrategy : ITicketPricingStrategy
    {
        public double CalculatePrice(double basePrice)
        {
            return basePrice; // Fără modificare
        }
    }

    public class VipPricingStrategy : ITicketPricingStrategy
    {
        public double CalculatePrice(double basePrice)
        {
            return basePrice * 0.8; // 20% reducere
        }
    }

    public class LastMinutePricingStrategy : ITicketPricingStrategy
    {
        public double CalculatePrice(double basePrice)
        {
            return basePrice * 1.5; // 50% adaos
        }
    }

    public class FrequentFlyerPricingStrategy : ITicketPricingStrategy
    {
        private int _points;
        public FrequentFlyerPricingStrategy(int points)
        {
            _points = points;
        }

        public double CalculatePrice(double basePrice)
        {
            double discount = (_points / 100) * 0.05; // 5% off per 100 points
            if (discount > 0.5) discount = 0.5; // Max 50% off
            return basePrice * (1.0 - discount);
        }
    }

    // Combined strategy: VIP Staff + Last Minute
    // VIP-uri nu platesc suprataxă de 50% — primesc un adaos de urgență redus (+15%) DAR cu reducerea VIP (-20%) aplicată
    public class VipLastMinutePricingStrategy : ITicketPricingStrategy
    {
        public double CalculatePrice(double basePrice)
        {
            // Last minute urgency: +15% (nu 50% — VIP au prioritate la seat reservation)
            // VIP discount: -20%
            // Net result: basePrice * 1.15 * 0.80 = basePrice * 0.92 → ~8% reducere față de prețul normal
            return basePrice * 1.15 * 0.80;
        }
    }

    // 3. Contextul
    public class TicketContext
    {
        private ITicketPricingStrategy _strategy;

        public TicketContext(ITicketPricingStrategy strategy)
        {
            _strategy = strategy;
        }

        public void SetStrategy(ITicketPricingStrategy strategy)
        {
            _strategy = strategy;
        }

        public double GetFinalPrice(double basePrice)
        {
            return _strategy.CalculatePrice(basePrice);
        }
    }
}
