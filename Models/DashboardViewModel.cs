using System.Collections.Generic;

namespace TMPP_Aeroport.Models
{
    public class DashboardViewModel
    {
        public int TotalFlights { get; set; }
        public int ActiveFlights { get; set; }
        public int TotalAircrafts { get; set; }
        public int TotalPassengers { get; set; }
        public int TotalTickets { get; set; }
        public double TotalRevenue { get; set; }
        public IEnumerable<Flight> RecentFlights { get; set; } = new List<Flight>();
    }
}
