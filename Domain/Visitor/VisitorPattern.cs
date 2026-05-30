using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Visitor
{
    // 1. Visitor Interface
    public interface IVisitor
    {
        void Visit(TerminalElement terminal);
        void Visit(AircraftElement aircraft);
        void Visit(FlightElement flight);
    }

    // 2. Element Interface
    public interface IAirportElement
    {
        void Accept(IVisitor visitor);
    }

    // 3. Concrete Elements
    public class TerminalElement : IAirportElement
    {
        public string Name { get; set; }
        public int NumberOfGates { get; set; }

        public TerminalElement(string name, int gates)
        {
            Name = name;
            NumberOfGates = gates;
        }

        public void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class AircraftElement : IAirportElement
    {
        public string Model { get; set; }
        public int Capacity { get; set; }

        public AircraftElement(string model, int capacity)
        {
            Model = model;
            Capacity = capacity;
        }

        public void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class FlightElement : IAirportElement
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; }
        public string Destination { get; set; }
        public System.DateTime DepartureTime { get; set; }

        public FlightElement(int id, string number, string destination, System.DateTime departureTime)
        {
            Id = id;
            FlightNumber = number;
            Destination = destination;
            DepartureTime = departureTime;
        }

        public void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    // 4. Concrete Visitors
    public class JsonExportVisitor : IVisitor
    {
        public List<string> ExportedData { get; } = new List<string>();

        public void Visit(TerminalElement terminal)
        {
            ExportedData.Add($"{{ \"Terminal\": \"{terminal.Name}\", \"Gates\": {terminal.NumberOfGates} }}");
        }

        public void Visit(AircraftElement aircraft)
        {
            ExportedData.Add($"{{ \"Aircraft\": \"{aircraft.Model}\", \"Capacity\": {aircraft.Capacity} }}");
        }

        public void Visit(FlightElement flight)
        {
            ExportedData.Add($"{{ \"Flight\": \"{flight.FlightNumber}\", \"Dest\": \"{flight.Destination}\", \"Time\": \"{flight.DepartureTime:yyyy-MM-dd HH:mm}\" }}");
        }
    }

    public class XmlExportVisitor : IVisitor
    {
        public List<string> ExportedData { get; } = new List<string>();

        public void Visit(TerminalElement terminal)
        {
            ExportedData.Add($"<Terminal Name=\"{terminal.Name}\" Gates=\"{terminal.NumberOfGates}\" />");
        }

        public void Visit(AircraftElement aircraft)
        {
            ExportedData.Add($"<Aircraft Model=\"{aircraft.Model}\" Capacity=\"{aircraft.Capacity}\" />");
        }

        public void Visit(FlightElement flight)
        {
            ExportedData.Add($"<Flight Number=\"{flight.FlightNumber}\" Dest=\"{flight.Destination}\" Time=\"{flight.DepartureTime:yyyy-MM-dd HH:mm}\" />");
        }
    }

    public class FlightDelayVisitor : IVisitor
    {
        private readonly System.TimeSpan _delayAmount;
        public List<FlightElement> DelayedFlights { get; } = new List<FlightElement>();

        public FlightDelayVisitor(System.TimeSpan delayAmount)
        {
            _delayAmount = delayAmount;
        }

        public void Visit(TerminalElement terminal)
        {
            // Nu se aplică
        }

        public void Visit(AircraftElement aircraft)
        {
            // Nu se aplică
        }

        public void Visit(FlightElement flight)
        {
            // Aplică delay la toate zborurile vizitate
            flight.DepartureTime = flight.DepartureTime.Add(_delayAmount);
            DelayedFlights.Add(flight);
        }
    }
}
