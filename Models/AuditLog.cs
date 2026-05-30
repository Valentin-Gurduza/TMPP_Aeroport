using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMPP_Aeroport.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "General"; // e.g. System, ATC, GroundOps, Security

        // Optional Reference to User
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
