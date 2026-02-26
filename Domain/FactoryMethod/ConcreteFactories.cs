using System;
using TMPP_Aeroport.Domain.Entities;

namespace TMPP_Aeroport.Domain.FactoryMethod
{
    // Concrete Creator 1: Fabrică pentru Avioane de Pasageri
    public class PassengerPlaneFactory : AircraftFactory
    {
        public override Aircraft CreateAircraft(string model, string regNum, dynamic extraData)
        {
            // extraData este interpretat ca numărul de locuri (int)
            int capacity = (int)extraData;
            return new PassengerPlane(model, regNum, capacity);
        }
    }

    // Concrete Creator 2: Fabrică pentru Avioane Cargo
    public class CargoPlaneFactory : AircraftFactory
    {
        public override Aircraft CreateAircraft(string model, string regNum, dynamic extraData)
        {
            // extraData este interpretat ca greutatea maximă (double)
            double maxWeight = (double)extraData;
            return new CargoPlane(model, regNum, maxWeight);
        }
    }
}
