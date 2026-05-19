using Microsoft.AspNetCore.SignalR;

namespace TMPP_Aeroport.Hubs
{
    public class FlightHub : Hub
    {
        // Hub-ul este un punct central prin care server-ul trimite apeluri 
        // JavaScript (WebSockets) catre toti clientii conectati in timp real.
        // Aici aplicam Observer Pattern distribuit!
    }
}
