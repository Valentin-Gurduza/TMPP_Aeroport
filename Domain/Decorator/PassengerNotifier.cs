using System;
using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Decorator
{
    // =========================================================
    // COMPONENTA DE BAZĂ (Interfața)
    // =========================================================
    public interface IBoardingNotifier
    {
        List<string> SendNotification(string passengerName, string message);
    }

    // =========================================================
    // CONCRETE COMPONENT
    // =========================================================
    // Sistemul standard "Core" care arată notificarea pe contul Web.
    public class WebAppNotifier : IBoardingNotifier
    {
        public List<string> SendNotification(string passengerName, string message)
        {
            List<string> logs = new List<string>();
            logs.Add($"[WEB] Trimis pe contul web aeroport pentru {passengerName}: {message}");
            return logs;
        }
    }

    // =========================================================
    // BASE DECORATOR
    // =========================================================
    // Clasa abstractă din care vor deriva toți decoratorii.
    // Ea "învelește" un IBoardingNotifier existent și delegă logica mai departe.
    public abstract class NotifierDecorator : IBoardingNotifier
    {
        protected IBoardingNotifier _wrappedNotifier;

        public NotifierDecorator(IBoardingNotifier notifier)
        {
            _wrappedNotifier = notifier;
        }

        public virtual List<string> SendNotification(string passengerName, string message)
        {
            // Delegăm execuția către clasa pe care o învelim
            return _wrappedNotifier.SendNotification(passengerName, message);
        }
    }

    // =========================================================
    // CONCRETE DECORATORS
    // =========================================================

    public class SMSNotifierDecorator : NotifierDecorator
    {
        public SMSNotifierDecorator(IBoardingNotifier notifier) : base(notifier) { }

        public override List<string> SendNotification(string passengerName, string message)
        {
            // 1. Cheamă logica de la bază (Web)
            List<string> logs = base.SendNotification(passengerName, message);
            
            // 2. Adaugă comportamentul NOU (Extensie dinamică OCP)
            logs.Add($"[SMS] Trimis pe telefonul lui {passengerName}: BEEP! {message}");
            return logs;
        }
    }

    public class EmailNotifierDecorator : NotifierDecorator
    {
        public EmailNotifierDecorator(IBoardingNotifier notifier) : base(notifier) { }

        public override List<string> SendNotification(string passengerName, string message)
        {
            // 1. Cheamă logica bazei (sau a decoratorului de sub el, dacă are mai multe straturi)
            List<string> logs = base.SendNotification(passengerName, message);
            
            // 2. Comportament NOU de mail
            logs.Add($"[EMAIL] Trimis la contactul lui {passengerName}: {message}");
            return logs;
        }
    }
}
