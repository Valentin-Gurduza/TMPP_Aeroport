using System;

namespace TMPP_Aeroport.Domain.Entities
{
    // SRP (Single Responsibility Principle): Această clasă are o singură responsabilitate: să rețină datele despre Zbor.
    // Prototype Pattern: Clasa implementează ICloneable pentru a permite duplicarea instanței.
    public class Flight : BaseEntity, ICloneable
    {
        public string FlightNumber { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public Guid AssignedAircraftId { get; set; }

        public Flight(string flightNumber, string origin, string destination)
        {
            Id = Guid.NewGuid(); // Asigurăm unic ID la creare
            FlightNumber = flightNumber;
            Origin = origin;
            Destination = destination;
        }

        // Prototype Pattern: Metoda de clonare (Shallow Copy)
        public object Clone()
        {
            // MemberwiseClone copiază direct valorile (string-uri, date, int-uri)
            var duplicate = (Flight)this.MemberwiseClone();
            
            // Re-generăm ID-ul pentru clonă, altfel ar avea același ID de bază de date ca originalul!
            duplicate.Id = Guid.NewGuid();
            
            return duplicate;
        }

        // Prototype Funcționalitate specifică: Clonarea zborului pentru format de tip "Daily Flight"
        public Flight CloneForNextDay()
        {
            var clonedFlight = (Flight)this.Clone();
            clonedFlight.DepartureTime = this.DepartureTime.AddDays(1);
            clonedFlight.ArrivalTime = this.ArrivalTime.AddDays(1);
            return clonedFlight;
        }
    }
}
