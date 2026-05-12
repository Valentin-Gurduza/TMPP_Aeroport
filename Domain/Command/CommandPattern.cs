using System.Collections.Generic;
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

        public void TurnOnLights()
        {
            Logs.Add("Runway lights: ON");
        }

        public void TurnOffLights()
        {
            Logs.Add("Runway lights: OFF");
        }

        public void ClearRunway()
        {
            Logs.Add("Runway cleared for takeoff/landing");
        }
        
        public void BlockRunway()
        {
            Logs.Add("Runway blocked");
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
