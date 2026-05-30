using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Builder
{
    // The Complex Product: Flight Itinerary
    // Un obiect complex cu mulți parametri (unele opționale).
    public class FlightItinerary : System.ICloneable
    {
        public string PassengerName { get; set; } = string.Empty;
        public string TicketType { get; set; } = string.Empty;
        public string SeatAssigned { get; set; } = string.Empty;
        public int CheckedBags { get; set; }
        public List<string> Meals { get; set; } = new List<string>();
        public bool HasLoungeAccess { get; set; }
        public bool HasPriorityBoarding { get; set; }

        public string GetItinerarySummary()
        {
            var summary = $"Itinerar pentru {PassengerName} ({TicketType}):\n" +
                          $"- Loc: {SeatAssigned}\n" +
                          $"- Bagaje de cală: {CheckedBags}\n" +
                          $"- Priority Boarding: {(HasPriorityBoarding ? "DA" : "NU")}\n" +
                          $"- Acces Lounge: {(HasLoungeAccess ? "DA" : "NU")}\n" +
                          $"- Meniu: {(Meals.Count > 0 ? string.Join(", ", Meals) : "Fără masă")}";
            return summary;
        }

        // Prototype Pattern Implementation
        public object Clone()
        {
            // Shallow copy is enough except for Lists
            var clone = (FlightItinerary)this.MemberwiseClone();
            clone.Meals = new List<string>(this.Meals);
            
            // To simulate booking for a group, we might clone the itinerary and just change the name/seat
            return clone;
        }
    }

    // Builder Interface
    // Definește pașii standard pentru construirea itinerariului.
    public interface IItineraryBuilder
    {
        void SetPassengerName(string name);
        void SetTicketType();
        void BuildSeatSelection();
        void BuildBaggageAllowance();
        void BuildMeals();
        void BuildExtraPerks();
        
        // Returnează produsul final
        FlightItinerary GetResult();
    }
}
