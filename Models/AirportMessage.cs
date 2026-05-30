using System;
using System.ComponentModel.DataAnnotations;

namespace TMPP_Aeroport.Models
{
    public class AirportMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string SenderFlight { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string MessageType { get; set; } = "Info"; // Emergency, Warning, Info

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsFromTower { get; set; } = false;

        public string? TowerResponse { get; set; }
    }
}
