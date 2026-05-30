using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

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

        // Optional: scope factory for DB persistence (set once at startup)
        private IServiceScopeFactory? _scopeFactory;

        // 1. Constructorul MEREU privat! Nicio altă clasă nu poate folosi 'new AirportLogger()'
        private AirportLogger()
        {
            _logs = new List<string>();
            _logs.Add($"[{DateTime.Now:HH:mm:ss}] [SYSTEM] Logger Singleton Initializat cu succes.");
        }

        // 2. Metoda / Proprietatea statică pentru acces global la instanța unică
        public static AirportLogger Instance
        {
            get { return _instance.Value; }
        }

        // Bridge between in-memory Singleton and DB AuditLogs — call once at startup from Program.cs
        public void Configure(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Metodele de business ale entității unice:
        public void Log(string message, string category = "System")
        {
            var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _logs.Add(formattedMessage);

            // Unified logging: also persist to DB if scope factory is configured
            if (_scopeFactory != null)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TMPP_Aeroport.Data.ApplicationDbContext>();
                    db.AuditLogs.Add(new TMPP_Aeroport.Models.AuditLog
                    {
                        Message = formattedMessage,
                        Category = category,
                        Timestamp = DateTime.UtcNow
                    });
                    db.SaveChanges();
                }
                catch
                {
                    // Fail silently — don't crash the app due to logging
                }
            }
        }

        public IReadOnlyList<string> GetLogs()
        {
            return _logs.AsReadOnly();
        }
    }
}
