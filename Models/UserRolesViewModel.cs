namespace TMPP_Aeroport.Models
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
