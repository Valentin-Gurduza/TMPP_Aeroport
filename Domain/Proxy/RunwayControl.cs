using System;
using TMPP_Aeroport.Domain.Singleton;

namespace TMPP_Aeroport.Domain.Proxy
{
    // =========================================================
    // SUBJECT INTERFACE
    // =========================================================
    public interface IRunwayControl
    {
        string GrantClearance(string aircraftId, string runwayName);
        string LockRunway(string runwayName);
    }

    // =========================================================
    // REAL SUBJECT
    // =========================================================
    // Clasa originală grea / critică (Business Logic adevărat)
    // Efectuează comenzile, dar nu știe nimic de "Securitate".
    public class RunwayControlService : IRunwayControl
    {
        public string GrantClearance(string aircraftId, string runwayName)
        {
            AirportLogger.Instance.Log($"[RealSubject] Avionul {aircraftId} primit clearance pt {runwayName}.");
            return $"AVERTISMENT VIZUAL: Avionul {aircraftId} este liber să intre pe {runwayName}.";
        }

        public string LockRunway(string runwayName)
        {
            AirportLogger.Instance.Log($"[RealSubject] {runwayName} a fost ÎNCUIATĂ. Nimeni nu mai poate decola.");
            return $"PISTA {runwayName} ESTE EMERGENCY LOCKED.";
        }
    }

    // =========================================================
    // PROTECTION PROXY
    // =========================================================
    // Intermediază Controllerul și RealSubject-ul. Pare a fi însuși turnul de control,
    // dar de fapt blochează apelurile dacă user-ul nu are permisiuni de Manager.
    public class RunwayControlProxy : IRunwayControl
    {
        private RunwayControlService _realSubject;
        private string _userRole;

        public RunwayControlProxy(string currentUserRole)
        {
            _userRole = currentUserRole;
            // Inițializare Leneșă (Lazy Load) e de asemenea posibilă aici.
        }

        private bool HasAccess()
        {
            // Verificăm permisiunile false
            return _userRole == "Admin" || _userRole == "ATC_Manager";
        }

        public string GrantClearance(string aircraftId, string runwayName)
        {
            if (HasAccess())
            {
                if (_realSubject == null) _realSubject = new RunwayControlService();
                return _realSubject.GrantClearance(aircraftId, runwayName);
            }

            AirportLogger.Instance.Log($"[PROXY BLOCK] Utilizatorul ({_userRole}) a încercat acces interzis GrantClearance.");
            return $"ACCES RESPINS: Tu ești `{_userRole}`. Doar Tower Managerii pot da clearance-uri!";
        }

        public string LockRunway(string runwayName)
        {
            if (HasAccess())
            {
                if (_realSubject == null) _realSubject = new RunwayControlService();
                return _realSubject.LockRunway(runwayName);
            }

            AirportLogger.Instance.Log($"[PROXY BLOCK] Utilizatorul ({_userRole}) a încercat să deactiveze pisa {runwayName}.");
            return $"CRITICAL: Nivel de securitate insuficient pentru acces direct la mecanismele pistelor.";
        }
    }
}
