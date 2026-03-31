using System;
using System.Collections.Generic;
using System.Linq;

namespace TMPP_Aeroport.Domain.Composite
{
    // ---------------------------------------------------------
    // "Component" (Interfața Comună)
    // Atât obiectele simple (Frunzele) cât și obiectele compuse 
    // trebuie să respecte aceeași regulă. Aici: calculul greutății.
    // ---------------------------------------------------------
    public interface ILuggageItem
    {
        string GetName();
        double GetWeight();
        
        // Polimorfism: Afișează o reprezentare arborescentă pe ecran
        string Display(int depth = 0); 
    }

    // ---------------------------------------------------------
    // "Leaf" (Nodurile Frunză - Obiectele Individuale simple)
    // Acestea nu pot conține alte obiecte. Reprezintă baza.
    // ---------------------------------------------------------
    public class Suitcase : ILuggageItem
    {
        private string _name;
        private double _weight;

        public Suitcase(string name, double weight)
        {
            _name = name;
            _weight = weight;
        }

        public string GetName() => _name;
        public double GetWeight() => _weight;

        public string Display(int depth = 0)
        {
            return new string('-', depth) + $" 🧳 {_name} ({_weight} kg)\n";
        }
    }

    public class Backpack : ILuggageItem
    {
        private string _name;
        private double _weight;

        public Backpack(string name, double weight)
        {
            _name = name;
            _weight = weight;
        }

        public string GetName() => _name;
        public double GetWeight() => _weight;

        public string Display(int depth = 0)
        {
            return new string('-', depth) + $" 🎒 {_name} ({_weight} kg)\n";
        }
    }

    // ---------------------------------------------------------
    // "Composite" (Nodul Compus)
    // Acest element este capabil să conțină atât Frunze (Suitcase)
    // cât și ALTE noduri Composite (Containers în Containers).
    // ---------------------------------------------------------
    public class LuggageContainer : ILuggageItem
    {
        private string _containerId;
        private List<ILuggageItem> _children = new List<ILuggageItem>();

        public LuggageContainer(string containerId)
        {
            _containerId = containerId;
        }

        public void Add(ILuggageItem item)
        {
            _children.Add(item);
        }

        public void Remove(ILuggageItem item)
        {
            _children.Remove(item);
        }

        public string GetName() => $"Container {_containerId}";

        // Aici este inima patternului Composite: Recursivitatea curată.
        // Când îi ceri greutatea, el o cere automat tuturor copiilor.
        public double GetWeight()
        {
            double totalWeight = 0;
            // Greutate proprie (ex: paletul de lemn are 15kg gol)
            totalWeight += 15.0; 

            // Se adună recursiv tot de sub el
            foreach (var item in _children)
            {
                totalWeight += item.GetWeight();
            }

            return totalWeight;
        }

        public string Display(int depth = 0)
        {
            string output = new string('-', depth) + $" 📦 {_containerId} (Greutate proprie 15 kg)\n";
            
            foreach (var item in _children)
            {
                output += item.Display(depth + 2);
            }
            return output;
        }
    }
}
