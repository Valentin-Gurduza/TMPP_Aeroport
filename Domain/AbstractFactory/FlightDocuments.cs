using System;

namespace TMPP_Aeroport.Domain.AbstractFactory
{
    // Abstract Product A: Boarding Pass
    // Definește comportamentul comun pentru tichetele de îmbarcare.
    public abstract class BoardingPass
    {
        public string PassengerName { get; set; }
        public string FlightNumber { get; set; }
        
        // Metodă abstractă pentru afișarea detaliilor specifice clasei (Economy/Business)
        public abstract string GetTicketDetails();
    }

    // Concrete Product A1: Economy Boarding Pass
    public class EconomyBoardingPass : BoardingPass
    {
        public override string GetTicketDetails()
        {
            return $"ECONOMY PASS - {PassengerName} (Zbor {FlightNumber}). Loc standard. Fără prioritate.";
        }
    }

    // Concrete Product A2: Business Boarding Pass
    public class BusinessBoardingPass : BoardingPass
    {
        public override string GetTicketDetails()
        {
            return $"BUSINESS PASS - {PassengerName} (Zbor {FlightNumber}). Loc premium. Priority Boarding inclus.";
        }
    }

    // Abstract Product B: Baggage Tag
    // Definește comportamentul comun pentru etichetele de bagaj.
    public abstract class BaggageTag
    {
        public string Code { get; set; }
        public abstract string GetTagColor();
    }

    // Concrete Product B1: Economy Tag
    public class EconomyBaggageTag : BaggageTag
    {
        public override string GetTagColor()
        {
            return "Alb (Standard)";
        }
    }

    // Concrete Product B2: Priority Tag (Business)
    public class PriorityBaggageTag : BaggageTag
    {
        public override string GetTagColor()
        {
            return "Roșu (PRIORITY - Manevrare rapidă)";
        }
    }
}
