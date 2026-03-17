using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Builder
{
    // The Complex Product: Flight Itinerary
    // Un obiect complex cu mulți parametri (unele opționale).
    public class FlightItinerary
    {
        public string PassengerName { get; set; }
        public string TicketType { get; set; }
        public string SeatAssigned { get; set; }
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
