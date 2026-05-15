using System.ComponentModel.DataAnnotations;

namespace TMPP_Aeroport.Models
{
    public class Aircraft
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string RegistrationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        public int Capacity { get; set; }

        [MaxLength(100)]
        public string Airline { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    }
}
