using System.Collections.Generic;
using TMPP_Aeroport.Domain.Entities;

namespace TMPP_Aeroport.Domain.Interfaces
{
    // ISP (Interface Segregation Principle): Clienții nu ar trebui să depindă de metode pe care nu le folosesc.
    // Separăm managementul Zborurilor de managementul Aeronavelor.

    public interface IFlightService
    {
        IEnumerable<Flight> GetAllFlights();
        void ScheduleFlight(Flight flight);
    }

    public interface IAircraftService
    {
        IEnumerable<Aircraft> GetAllAircraft();
        void RegisterAircraft(Aircraft aircraft);
    }
}
