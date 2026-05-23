using Microsoft.AspNetCore.SignalR;

namespace TMPP_Aeroport.Hubs
{
    public class FlightHub : Hub
    {
        // Hub-ul este un punct central prin care server-ul trimite apeluri 
        // JavaScript (WebSockets) catre toti clientii conectati in timp real.
        // Aici aplicam Observer Pattern distribuit!

        public async Task SetSpeedMultiplier(int speed)
        {
            // Update the global speed multiplier in the background service
            TMPP_Aeroport.Services.FlightSimulationService.GlobalSpeedMultiplier = speed;
            await Clients.All.SendAsync("SpeedMultiplierUpdated", speed);
        }
    }
}
