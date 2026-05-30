using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMPP_Aeroport.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [MaxLength(10)]
        public string SeatNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string TicketState { get; set; } = "WaitingForPayment"; // Expected: WaitingForPayment, PaymentProcessing, Issued, CheckedIn, Boarded, Completed, Cancelled

        [MaxLength(20)]
        public string SecurityStatus { get; set; } = "Pending";

        public DateTime? CheckInAt { get; set; }

        public DateTime? BoardedAt { get; set; }

        [MaxLength(50)]
        public string FareClass { get; set; } = "Economy";

        public int BaggageWeight { get; set; }

        [MaxLength(50)]
        public string StrategyApplied { get; set; } = string.Empty;

        // Foreign Key to Flight
        public int FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight? Flight { get; set; }

        // Foreign Key to ApplicationUser (Passenger)
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
