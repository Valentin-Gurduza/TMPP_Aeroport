using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMPP_Aeroport.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        public decimal Price { get; set; }

        [MaxLength(10)]
        public string SeatNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string TicketState { get; set; } = "WaitingForPayment"; // Expected: WaitingForPayment, Validating, Issued

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
