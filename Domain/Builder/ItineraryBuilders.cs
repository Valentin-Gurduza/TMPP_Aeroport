using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Builder
{
    // Concrete Builder: Economy
    public class EconomyItineraryBuilder : IItineraryBuilder
    {
        private FlightItinerary _itinerary = new FlightItinerary();
        private string _passengerName;

        public EconomyItineraryBuilder()
        {
            this.Reset();
        }

        public void Reset()
        {
            _itinerary = new FlightItinerary();
        }

        public void SetPassengerName(string name)
        {
            _itinerary.PassengerName = name;
        }

        public void SetTicketType()
        {
            _itinerary.TicketType = "Economy";
        }

        public void BuildSeatSelection()
        {
            _itinerary.SeatAssigned = "Loc Standard (Aleatoriu)";
        }

        public void BuildBaggageAllowance()
        {
            _itinerary.CheckedBags = 1; // 1 Bagaj inclus
        }

        public void BuildMeals()
        {
            _itinerary.Meals.Add("Apă și Snack-uri");
        }

        public void BuildExtraPerks()
        {
            _itinerary.HasLoungeAccess = false;
            _itinerary.HasPriorityBoarding = false;
        }

        public FlightItinerary GetResult()
        {
            FlightItinerary result = _itinerary;
            this.Reset(); // Pregătește builder-ul pentru alt produs
            return result;
        }
    }

    // Concrete Builder: Business
    public class BusinessItineraryBuilder : IItineraryBuilder
    {
        private FlightItinerary _itinerary = new FlightItinerary();

        public BusinessItineraryBuilder()
        {
            this.Reset();
        }

        public void Reset()
        {
            _itinerary = new FlightItinerary();
        }

        public void SetPassengerName(string name)
        {
            _itinerary.PassengerName = name;
        }

        public void SetTicketType()
        {
            _itinerary.TicketType = "Business Class";
        }

        public void BuildSeatSelection()
        {
            _itinerary.SeatAssigned = "Loc Premium (Rândul 1, Fereastră)";
        }

        public void BuildBaggageAllowance()
        {
            _itinerary.CheckedBags = 3; // 3 bagaje incluse
        }

        public void BuildMeals()
        {
            _itinerary.Meals.Add("Șampanie de bun venit");
            _itinerary.Meals.Add("Meniu cald complet");
        }

        public void BuildExtraPerks()
        {
            _itinerary.HasLoungeAccess = true;
            _itinerary.HasPriorityBoarding = true;
        }

        public FlightItinerary GetResult()
        {
            FlightItinerary result = _itinerary;
            this.Reset();
            return result;
        }
    }

    // Director: Orchestrează apelurile pașilor
    public class ItineraryDirector
    {
        private IItineraryBuilder _builder;

        public ItineraryDirector(IItineraryBuilder builder)
        {
            _builder = builder;
        }

        // Metodă opțională pentru a schimba builder-ul din mers
        public void ChangeBuilder(IItineraryBuilder builder)
        {
            _builder = builder;
        }

        // Construiește un itinerar complet standard
        public void ConstructFullItinerary(string passengerName)
        {
            _builder.SetPassengerName(passengerName);
            _builder.SetTicketType();
            _builder.BuildSeatSelection();
            _builder.BuildBaggageAllowance();
            _builder.BuildMeals();
            _builder.BuildExtraPerks();
        }

        // Poți avea și metode care construiesc o versiune "Light"
        public void ConstructNoBaggageItinerary(string passengerName)
        {
            _builder.SetPassengerName(passengerName);
            _builder.SetTicketType();
            _builder.BuildSeatSelection();
            // Sare peste bagaje și extra perks
        }
    }
}
