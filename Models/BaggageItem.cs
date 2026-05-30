using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMPP_Aeroport.Models
{
    public class BaggageItem
    {
        [Key]
        public int Id { get; set; }

        public int FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight? Flight { get; set; }

        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int Weight { get; set; }

        [MaxLength(50)]
        public string Type { get; set; } = "Suitcase"; // Suitcase, Backpack, Oversized

        [MaxLength(20)]
        public string TagCode { get; set; } = string.Empty;

        [MaxLength(20)]
        public string SecurityStatus { get; set; } = "Pending"; // Pending, Cleared, Flagged, Rejected

        [MaxLength(30)]
        public string BaggageStage { get; set; } = "CheckedIn"; // CheckedIn, OnConveyor, XRayScreening, Sorted, LoadedOnAircraft

        public DateTime? StageUpdatedAt { get; set; }
    }
}
