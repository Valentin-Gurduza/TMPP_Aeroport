using System.Collections.Generic;
//Ofera un mod de accesa secventil elementele  unui obiect agregat
//Fara a expune structura interna

namespace TMPP_Aeroport.Domain.Iterator
{
    // Modelul pe care îl vom itera
    public class FlightScheduleItem
    {
        public string FlightNumber { get; set; } = string.Empty;
        public string Terminal { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // 1. Interfața Iterator
    public interface IFlightIterator
    {
        bool HasNext();
        FlightScheduleItem? Next();
    }

    // 2. Interfața Colecției
    public interface IFlightCollection
    {
        IFlightIterator CreateIterator();
        IFlightIterator CreateTerminalIterator(string terminal);
    }

    // 3. Colecția Concretă
    public class FlightScheduleCollection : IFlightCollection
    {
        private List<FlightScheduleItem> _flights = new List<FlightScheduleItem>();

        public void AddFlight(FlightScheduleItem flight)
        {
            _flights.Add(flight);
        }

        public List<FlightScheduleItem> GetItems()
        {
            return _flights;
        }

        public IFlightIterator CreateIterator()
        {
            return new AllFlightsIterator(this);
        }

        public IFlightIterator CreateTerminalIterator(string terminal)
        {
            return new TerminalFlightsIterator(this, terminal);
        }
    }

    // 4. Iterator Concret (Toate Zborurile)
    public class AllFlightsIterator : IFlightIterator
    {
        private FlightScheduleCollection _collection;
        private int _position = -1;

        public AllFlightsIterator(FlightScheduleCollection collection)
        {
            _collection = collection;
        }

        public bool HasNext()
        {
            return _position < _collection.GetItems().Count - 1;
        }

        public FlightScheduleItem? Next()
        {
            _position++;
            return _collection.GetItems()[_position];
        }
    }

    // 5. Iterator Concret (Zboruri filtrate după Terminal)
    public class TerminalFlightsIterator : IFlightIterator
    {
        private FlightScheduleCollection _collection;
        private string _targetTerminal;
        private int _position = -1;

        public TerminalFlightsIterator(FlightScheduleCollection collection, string targetTerminal)
        {
            _collection = collection;
            _targetTerminal = targetTerminal;
        }

        public bool HasNext()
        {
            int tempPosition = _position + 1;
            while (tempPosition < _collection.GetItems().Count)
            {
                if (_collection.GetItems()[tempPosition].Terminal == _targetTerminal)
                {
                    return true;
                }
                tempPosition++;
            }
            return false;
        }

        public FlightScheduleItem? Next()
        {
            _position++;
            while (_position < _collection.GetItems().Count)
            {
                var item = _collection.GetItems()[_position];
                if (item.Terminal == _targetTerminal)
                {
                    return item;
                }
                _position++;
            }
            return null;
        }
    }
}
