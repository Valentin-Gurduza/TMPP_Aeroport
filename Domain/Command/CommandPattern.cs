using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
// Incapsuleaza o cerere ca un obiect...6

namespace TMPP_Aeroport.Domain.Command
{
    // 1. Interfața Command
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    // 2. Receiver (cel care execută logica reală)
    public class RunwayReceiver
    {
        public List<string> Logs { get; } = new List<string>();
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

        public RunwayReceiver(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        private void SaveLog(string message)
        {
            Logs.Add(message);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TMPP_Aeroport.Data.ApplicationDbContext>();
            db.AuditLogs.Add(new TMPP_Aeroport.Models.AuditLog { Message = message, Category = "ATC Command" });
            db.SaveChanges();
        }

        public void TurnOnLights()
        {
            SaveLog("Runway lights: ON");
        }

        public void TurnOffLights()
        {
            SaveLog("Runway lights: OFF");
        }

        public void ClearRunway()
        {
            SaveLog("Runway cleared for takeoff/landing");
        }
        
        public void BlockRunway()
        {
            SaveLog("Runway blocked");
        }
    }

    // 3. Comenzile Concrete
    public class ToggleLightsCommand : ICommand
    {
        private RunwayReceiver _receiver;
        private bool _isCurrentlyOn;

        public ToggleLightsCommand(RunwayReceiver receiver, bool isCurrentlyOn)
        {
            _receiver = receiver;
            _isCurrentlyOn = isCurrentlyOn;
        }

        public void Execute()
        {
            if (_isCurrentlyOn)
                _receiver.TurnOffLights();
            else
                _receiver.TurnOnLights();
        }

        public void Undo()
        {
            // Efectul opus
            if (_isCurrentlyOn)
                _receiver.TurnOnLights();
            else
                _receiver.TurnOffLights();
        }
    }

    public class PrepareRunwayCommand : ICommand
    {
        private RunwayReceiver _receiver;

        public PrepareRunwayCommand(RunwayReceiver receiver)
        {
            _receiver = receiver;
        }

        public void Execute()
        {
            _receiver.ClearRunway();
            _receiver.TurnOnLights();
        }

        public void Undo()
        {
            _receiver.TurnOffLights();
            _receiver.BlockRunway();
        }
    }

    // MacroCommand (Composite Pattern within Command)
    public class EmergencyMacroCommand : ICommand
    {
        private List<ICommand> _commands = new List<ICommand>();
        private RunwayReceiver _receiver;

        public EmergencyMacroCommand(RunwayReceiver receiver)
        {
            _receiver = receiver;
        }

        public void AddCommand(ICommand command)
        {
            _commands.Add(command);
        }

        public void Execute()
        {
            _receiver.BlockRunway();
            _receiver.TurnOnLights(); // Red lights simulation
            
            foreach (var cmd in _commands)
            {
                cmd.Execute();
            }
        }

        public void Undo()
        {
            // Undo in reverse order
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
            _receiver.ClearRunway();
        }
    }

    // 4. Invoker (stochează și apelează comenzile)
    public class AtcInvoker
    {
        private Stack<ICommand> _commandHistory = new Stack<ICommand>();

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _commandHistory.Push(command);
        }

        public void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                var command = _commandHistory.Pop();
                command.Undo();
            }
        }
    }
}
