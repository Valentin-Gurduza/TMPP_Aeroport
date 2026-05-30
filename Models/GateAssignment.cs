using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMPP_Aeroport.Models
{
    public class GateAssignment
    {
        [Key]
        public int Id { get; set; }

        public int FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight? Flight { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gate { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string AssignedBy { get; set; } = string.Empty; // Username of the ground staff/admin

        public bool IsActive { get; set; } = true;
    }
}
