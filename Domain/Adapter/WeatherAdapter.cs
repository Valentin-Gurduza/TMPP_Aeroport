using System;

namespace TMPP_Aeroport.Domain.Adapter
{
    // SRP & ISP: O interfață simplă, curată, de care are nevoie clientul (aplicația noastră MVC)
    // Așteaptă întotdeauna rezultatul în formatul european standard (Celsius).
    public interface IAirportWeatherService
    {
        double GetTemperatureCelsius(string city);
    }

    // ---------------------------------------------------------
    // "Adaptee" (Clasa Incompatibilă / Legacy)
    // O simulăm ca pe un API extern care furnizează temperatura 
    // doar în grade Kelvin, pe care nu avem voie sau nu putem să o modificăm.
    // ---------------------------------------------------------
    public class LegacyWeatherSystem
    {
        // Metodă care nu se potrivește cu contractul IAirportWeatherService
        public double GetTemperatureKelvin(string cityCode)
        {
            // Simulăm un apel la un API extern (Hardcodat pentru demo)
            if (cityCode.ToUpper() == "BBU" || cityCode.ToUpper() == "OTP")
            {
                // ~ 25 grade Celsius = 298.15 Kelvin
                return 298.15;
            }
            if (cityCode.ToUpper() == "LHR") // London
            {
                // ~ 15 grade Celsius = 288.15 Kelvin
                return 288.15;
            }
            return 273.15; // Default 0 grade Celsius
        }
    }

    // ---------------------------------------------------------
    // "Adapter" (Adaptorul)
    // Implementează interfața cerută de noi, și se folosește
    // interactiv de obiectul Legacy pentru a extrage și traduce datale.
    // ---------------------------------------------------------
    public class WeatherAdapter : IAirportWeatherService
    {
        private readonly LegacyWeatherSystem _legacySystem;

        public WeatherAdapter(LegacyWeatherSystem legacySystem)
        {
            _legacySystem = legacySystem;
        }

        public double GetTemperatureCelsius(string city)
        {
            // 1. Apelăm sistemul vechi (care are un alt comportament/input)
            // Sistemul legacy vrea coduri IATA în loc de nume pline, 
            // aici putem face și adaptare de parametri dacă e necesar.
            string cityCode = city.Length > 3 ? city.Substring(0, 3).ToUpper() : city;
            
            double kelvinTemp = _legacySystem.GetTemperatureKelvin(cityCode);

            // 2. Traducem / Adaptăm datele din Kelvin în Celsius
            double celsiusTemp = kelvinTemp - 273.15;
            
            // 3. Jurnalizăm acțiunea (Singleton demo anterior)
            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log($"[Adapter] S-a tradus temp pt {city}: {kelvinTemp}K -> {Math.Round(celsiusTemp, 1)}°C");

            return Math.Round(celsiusTemp, 1);
        }
    }
}
