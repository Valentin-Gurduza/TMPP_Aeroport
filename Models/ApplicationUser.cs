using Microsoft.AspNetCore.Identity;

namespace TMPP_Aeroport.Models
{
    // Extends the default IdentityUser to add custom properties
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmployeeId { get; set; } // Nullable, as passengers won't have it
    }
}
