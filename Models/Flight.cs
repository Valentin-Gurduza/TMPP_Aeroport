using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMPP_Aeroport.Models
{
    public class Flight : ICloneable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Origin { get; set; } = "Bucharest OTP";

        [Required]
        [MaxLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = FlightStatus.Scheduled;

        public DateTime? ArrivalTime { get; set; }

        [MaxLength(10)]
        public string Terminal { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Gate { get; set; } = string.Empty;

        public int MaxCapacity { get; set; }

        public int BaggageLimitKg { get; set; }

        // Foreign Key to Aircraft
        public int AircraftId { get; set; }
        [ForeignKey("AircraftId")]
        public Aircraft? Aircraft { get; set; }

        // Navigation property
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        // Prototype Pattern: Metoda de clonare (Shallow Copy)
        public object Clone()
        {
            var duplicate = (Flight)this.MemberwiseClone();
            // Reset fields that must be unique
            duplicate.Id = 0; 
            duplicate.Tickets = new List<Ticket>();
            return duplicate;
        }
    }
}
