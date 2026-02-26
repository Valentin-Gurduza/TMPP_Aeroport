using TMPP_Aeroport.Domain.Entities;

namespace TMPP_Aeroport.Domain.FactoryMethod
{
    // Factory Method Pattern: Creator Abstract
    // Definește metoda abstractă de fabricare a obiectelor.
    public abstract class AircraftFactory
    {
        // Metoda Factory: Returnează un produs abstract (Aircraft).
        // Subclasele vor decide ce tip concret de avion să creeze.
        public abstract Aircraft CreateAircraft(string model, string regNum, dynamic extraData);
    }
}
