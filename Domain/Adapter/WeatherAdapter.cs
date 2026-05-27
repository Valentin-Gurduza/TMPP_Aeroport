using System;

namespace TMPP_Aeroport.Domain.Adapter
{
    // SRP & ISP: O interfață simplă, curată, de care are nevoie clientul (aplicația noastră MVC)
    // Așteaptă întotdeauna rezultatul în formatul european standard (Celsius).
    public interface IAirportWeatherService
    {
        double GetTemperatureCelsius(string city);
    }

    // "Adaptee" (Clasa Incompatibilă / Legacy)
    // O simulăm ca pe un API extern care furnizează temperatura 
    // doar în grade Kelvin, pe care nu avem voie sau nu putem să o modificăm.
    public class LegacyWeatherSystem
    {
        // Metodă care nu se potrivește cu contractul IAirportWeatherService
        public double GetTemperatureKelvin(string cityCode)
        {
            // O bază de date legacy care stochează temperaturile în grade Kelvin
            return cityCode.ToUpper() switch
            {
                "OTP" => 298.15, // Bucharest ~ 25 C
                "BBU" => 298.15, // Bucharest ~ 25 C
                "CDG" => 293.15, // Paris ~ 20 C
                "FRA" => 290.15, // Frankfurt ~ 17 C
                "LHR" => 288.15, // London ~ 15 C
                "AMS" => 287.15, // Amsterdam ~ 14 C
                "FCO" => 295.15, // Rome ~ 22 C
                "MAD" => 301.15, // Madrid ~ 28 C
                "BER" => 289.15, // Berlin ~ 16 C
                _ => 273.15     // Default 0 C (Freezing point)
            };
        }
    }

    // "Adapter" (Adaptorul)
    // Implementează interfața cerută de noi, și se folosește
    // interactiv de obiectul Legacy pentru a extrage și traduce datele.
    public class WeatherAdapter : IAirportWeatherService
    {
        private readonly LegacyWeatherSystem _legacySystem;

        public WeatherAdapter(LegacyWeatherSystem legacySystem)
        {
            _legacySystem = legacySystem;
        }

        public double GetTemperatureCelsius(string city)
        {
            if (string.IsNullOrEmpty(city)) return 15.0;

            // 1. Traducem / adaptăm parametrii primiți (nume pline de orașe din simulation service)
            // în coduri IATA pe care le recunoaște sistemul legacy.
            string normalizedCity = city.Trim().ToUpper();
            string cityCode;

            if (normalizedCity.Contains("BUCHAREST") || normalizedCity.Contains("OTP") || normalizedCity.Contains("BBU"))
                cityCode = "OTP";
            else if (normalizedCity.Contains("PARIS") || normalizedCity.Contains("CDG"))
                cityCode = "CDG";
            else if (normalizedCity.Contains("FRANKFURT") || normalizedCity.Contains("FRA"))
                cityCode = "FRA";
            else if (normalizedCity.Contains("LONDON") || normalizedCity.Contains("LHR"))
                cityCode = "LHR";
            else if (normalizedCity.Contains("AMSTERDAM") || normalizedCity.Contains("AMS"))
                cityCode = "AMS";
            else if (normalizedCity.Contains("ROME") || normalizedCity.Contains("FCO"))
                cityCode = "FCO";
            else if (normalizedCity.Contains("MADRID") || normalizedCity.Contains("MAD"))
                cityCode = "MAD";
            else if (normalizedCity.Contains("BERLIN") || normalizedCity.Contains("BER"))
                cityCode = "BER";
            else
                cityCode = city.Length > 3 ? city.Substring(0, 3).ToUpper() : city;

            // 2. Apelăm sistemul vechi cu parametrul adaptat
            double kelvinTemp = _legacySystem.GetTemperatureKelvin(cityCode);

            // 3. Traducem / Adaptăm datele din Kelvin în Celsius
            double celsiusTemp = kelvinTemp - 273.15;
            
            // 4. Jurnalizăm acțiunea (Singleton demo)
            TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Log($"[Adapter] S-a tradus temp pt '{city}' (cod adaptat: {cityCode}): {kelvinTemp}K -> {Math.Round(celsiusTemp, 1)}°C");

            return Math.Round(celsiusTemp, 1);
        }
    }
}
