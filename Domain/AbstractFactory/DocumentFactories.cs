namespace TMPP_Aeroport.Domain.AbstractFactory
{
    // Abstract Factory Interface
    // Declară metode pentru crearea unei FAMILII de produse (Bilet + Etichetă Bagaj).
    // Asigură că produsele create sunt compatibile între ele.
    public interface IFlightDocumentFactory
    {
        BoardingPass CreateBoardingPass();
        BaggageTag CreateBaggageTag();
    }

    // Concrete Factory 1: Economy Factory
    // Creează familia de produse pentru clasa Economy.
    public class EconomyDocumentFactory : IFlightDocumentFactory
    {
        public BoardingPass CreateBoardingPass()
        {
            return new EconomyBoardingPass();
        }

        public BaggageTag CreateBaggageTag()
        {
            return new EconomyBaggageTag();
        }
    }

    // Concrete Factory 2: Business Factory
    // Creează familia de produse pentru clasa Business.
    public class BusinessDocumentFactory : IFlightDocumentFactory
    {
        public BoardingPass CreateBoardingPass()
        {
            return new BusinessBoardingPass();
        }

        public BaggageTag CreateBaggageTag()
        {
            return new PriorityBaggageTag();
        }
    }
}
