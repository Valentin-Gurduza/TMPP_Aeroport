using System;

namespace TMPP_Aeroport.Domain.Entities
{
    // SRP (Single Responsibility Principle): Această clasă are o singură responsabilitate: să rețină datele despre Zbor.
    // Nu conține logică de rezervare, salvare în baza de date sau afișare.
    public class Flight : BaseEntity
    {
        public string FlightNumber { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public Guid AssignedAircraftId { get; set; }

        public Flight(string flightNumber, string origin, string destination)
        {
            FlightNumber = flightNumber;
            Origin = origin;
            Destination = destination;
        }
    }
}
