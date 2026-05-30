namespace TMPP_Aeroport.Domain.Strategy
{
    // Gate categories
    public enum GateCategory { Heavy, NarrowBody, Regional }

    // Gate Allocation Strategy Interface
    public interface IGateAllocationStrategy
    {
        GateCategory Category { get; }
        string AllocateGate(List<string> occupiedGates);
    }

    // Heavy aircraft (B777, B747, A380) - Terminal 1, Gates A01-A10
    public class HeavyGateStrategy : IGateAllocationStrategy
    {
        public GateCategory Category => GateCategory.Heavy;
        private static readonly List<string> HeavyGates = new()
        {
            "A01", "A02", "A03", "A04", "A05",
            "A06", "A07", "A08", "A09", "A10"
        };

        public string AllocateGate(List<string> occupiedGates)
        {
            return HeavyGates.FirstOrDefault(g => !occupiedGates.Contains(g)) ?? "A01";
        }
    }

    // Narrow body (A320, B737) - Terminal 2, Gates B01-B10
    public class NarrowBodyGateStrategy : IGateAllocationStrategy
    {
        public GateCategory Category => GateCategory.NarrowBody;
        private static readonly List<string> NarrowGates = new()
        {
            "B01", "B02", "B03", "B04", "B05",
            "B06", "B07", "B08", "B09", "B10"
        };

        public string AllocateGate(List<string> occupiedGates)
        {
            return NarrowGates.FirstOrDefault(g => !occupiedGates.Contains(g)) ?? "B01";
        }
    }

    // Regional / small aircraft - Terminal 3, Gates C01-C08
    public class RegionalGateStrategy : IGateAllocationStrategy
    {
        public GateCategory Category => GateCategory.Regional;
        private static readonly List<string> RegionalGates = new()
        {
            "C01", "C02", "C03", "C04",
            "C05", "C06", "C07", "C08"
        };

        public string AllocateGate(List<string> occupiedGates)
        {
            return RegionalGates.FirstOrDefault(g => !occupiedGates.Contains(g)) ?? "C01";
        }
    }

    // Smart Gate Allocator - picks strategy based on aircraft model
    public class SmartGateAllocator
    {
        private static readonly HashSet<string> HeavyModels = new(StringComparer.OrdinalIgnoreCase)
        { "B777", "B747", "A380", "A350", "B787", "Boeing 777", "Boeing 747", "Airbus A380" };
        private static readonly HashSet<string> RegionalModels = new(StringComparer.OrdinalIgnoreCase)
        { "ATR72", "E190", "CRJ900", "Q400", "ATR 72", "Embraer 190" };

        public string AllocateGate(string aircraftModel, List<string> occupiedGates)
        {
            IGateAllocationStrategy strategy;
            if (HeavyModels.Any(m => aircraftModel.Contains(m, StringComparison.OrdinalIgnoreCase)))
                strategy = new HeavyGateStrategy();
            else if (RegionalModels.Any(m => aircraftModel.Contains(m, StringComparison.OrdinalIgnoreCase)))
                strategy = new RegionalGateStrategy();
            else
                strategy = new NarrowBodyGateStrategy();

            return strategy.AllocateGate(occupiedGates);
        }

        public string GetCategoryLabel(string aircraftModel)
        {
            if (HeavyModels.Any(m => aircraftModel.Contains(m, StringComparison.OrdinalIgnoreCase)))
                return "Heavy (Terminal 1)";
            if (RegionalModels.Any(m => aircraftModel.Contains(m, StringComparison.OrdinalIgnoreCase)))
                return "Regional (Terminal 3)";
            return "Narrow-Body (Terminal 2)";
        }
    }
}
