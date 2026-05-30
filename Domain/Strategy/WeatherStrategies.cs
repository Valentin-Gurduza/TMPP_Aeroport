namespace TMPP_Aeroport.Domain.Strategy
{
    // Weather Strategy Pattern - affects ATC landing rules
    public enum WeatherType { Clear, Fog, Storm }

    public interface IWeatherStrategy
    {
        WeatherType WeatherType { get; }
        string Description { get; }
        bool CanLand { get; }
        bool CanTakeoff { get; }
        int VisibilityMeters { get; }
        int FuelBurnMultiplier { get; } // 1 = normal, 2 = double consumption in holding
    }

    public class ClearWeatherStrategy : IWeatherStrategy
    {
        public WeatherType WeatherType => WeatherType.Clear;
        public string Description => "Clear skies - all operations normal";
        public bool CanLand => true;
        public bool CanTakeoff => true;
        public int VisibilityMeters => 10000;
        public int FuelBurnMultiplier => 1;
    }

    public class FogWeatherStrategy : IWeatherStrategy
    {
        public WeatherType WeatherType => WeatherType.Fog;
        public string Description => "Dense fog - reduced visibility, ILS required";
        public bool CanLand => true; // ILS allows landing
        public bool CanTakeoff => true;
        public int VisibilityMeters => 200;
        public int FuelBurnMultiplier => 1;
    }

    public class StormWeatherStrategy : IWeatherStrategy
    {
        public WeatherType WeatherType => WeatherType.Storm;
        public string Description => "Severe storm - all landings suspended";
        public bool CanLand => false;
        public bool CanTakeoff => false;
        public int VisibilityMeters => 50;
        public int FuelBurnMultiplier => 2;
    }
}
