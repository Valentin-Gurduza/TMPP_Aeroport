using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMPP_Aeroport.Models
{
    public class Flight
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Scheduled"; // Expected: Scheduled, Boarding, Airborne, Landed, Cancelled

        // Foreign Key to Aircraft
        public int AircraftId { get; set; }
        [ForeignKey("AircraftId")]
        public Aircraft? Aircraft { get; set; }

        // Navigation property
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
