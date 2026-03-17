using System;
using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Singleton
{
    // Singleton Pattern: O singură instanță globală (Thread-Safe folosind Lazy<T>)
    // 'sealed' previne moștenirea, care ar putea crea instanțe adiționale nedorite
    public sealed class AirportLogger
    {
        // Lazy<T> garantează instanțierea Thread-Safe și întârziată (Lazy Initialization)
        private static readonly Lazy<AirportLogger> _instance = 
            new Lazy<AirportLogger>(() => new AirportLogger());

        // O listă internă pentru a ține evidența log-urilor în memorie pe parcursul rulării
        private readonly List<string> _logs;

        // 1. Constructorul MEREU privat! Nicio altă clasă nu poate folosi 'new AirportLogger()'
        private AirportLogger()
        {
            _logs = new List<string>();
            _logs.Add($"[{DateTime.Now:HH:mm:ss}] [SYSTEM] Logger Singleton Initializat cu succes.");
        }

        // 2. Metoda / Proprietatea statică pentru acces global la instanța unică
        public static AirportLogger Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        // Metodele de business ale entității unice:
        public void Log(string message)
        {
            var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _logs.Add(formattedMessage);
            
            // Console.WriteLine(formattedMessage);
        }

        public IReadOnlyList<string> GetLogs()
        {
            return _logs.AsReadOnly();
        }
    }
}
