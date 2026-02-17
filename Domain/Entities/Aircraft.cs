using System;

namespace TMPP_Aeroport.Domain.Entities
{
    // OCP (Open/Closed Principle): Această clasă este deschisă pentru extindere (ex: putem adăuga Elicoptere),
    // dar închisă pentru modificare (nu trebuie să modificăm codul existent pentru a adăuga tipuri noi).
    public abstract class Aircraft : BaseEntity
    {
        public string Model { get; set; }
        public string RegistrationNumber { get; set; }

        protected Aircraft(string model, string registrationNumber)
        {
            Model = model;
            RegistrationNumber = registrationNumber;
        }

        public abstract string GetAircraftType();
    }

    // Moștenire & Polimorfism: Clasa PassengerPlane moștenește Aircraft și implementează comportamentul specific.
    public class PassengerPlane : Aircraft
    {
        public int PassengerCapacity { get; set; }

        public PassengerPlane(string model, string registrationNumber, int capacity) 
            : base(model, registrationNumber)
        {
            PassengerCapacity = capacity;
        }

        public override string GetAircraftType()
        {
            return "Avion de Pasageri";
        }
    }

    // LSP (Liskov Substitution Principle): Obiectele de tip CargoPlane pot înlocui obiectele de tip Aircraft
    // oriunde în aplicație fără a afecta corectitudinea programului.
    public class CargoPlane : Aircraft
    {
        public double MaxCargoWeightKg { get; set; }

        public CargoPlane(string model, string registrationNumber, double maxCargoWeight) 
            : base(model, registrationNumber)
        {
            MaxCargoWeightKg = maxCargoWeight;
        }

        public override string GetAircraftType()
        {
            return "Avion Cargo";
        }
    }
}
