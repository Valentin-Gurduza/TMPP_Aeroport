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
