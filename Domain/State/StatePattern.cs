using System;
using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.State
{
    // 1. Context Class
    public class TicketStateContext
    {
        public TMPP_Aeroport.Models.Ticket Ticket { get; }
        private ITicketState _state;

        public TicketStateContext(TMPP_Aeroport.Models.Ticket ticket)
        {
            Ticket = ticket;
            _state = GetStateObject(ticket.TicketState);
        }

        private ITicketState GetStateObject(string stateStr)
        {
            return stateStr switch
            {
                "WaitingForPayment" => new WaitingForPaymentState(),
                "PaymentProcessing" => new PaymentProcessingState(),
                "Issued" => new IssuedState(),
                "CheckedIn" => new CheckedInState(),
                "Boarded" => new BoardedState(),
                "Cancelled" => new CancelledState(),
                "Refunded" => new RefundedState(),
                _ => new WaitingForPaymentState()
            };
        }

        public void ChangeState(ITicketState newState)
        {
            _state = newState;
            Ticket.TicketState = newState.GetType().Name.Replace("State", "");
        }

        public void Pay() => _state.Pay(this);
        public void Issue() => _state.Issue(this);
        public void CheckIn() => _state.CheckIn(this);
        public void Board() => _state.Board(this);
        public void Cancel() => _state.Cancel(this);
        public void Refund() => _state.Refund(this);
    }

    // 2. State Interface
    public interface ITicketState
    {
        void Pay(TicketStateContext context);
        void Issue(TicketStateContext context);
        void CheckIn(TicketStateContext context);
        void Board(TicketStateContext context);
        void Cancel(TicketStateContext context);
        void Refund(TicketStateContext context);
    }

    // 3. Concrete States
    public class WaitingForPaymentState : ITicketState
    {
        public void Pay(TicketStateContext context) => context.ChangeState(new PaymentProcessingState());
        public void Issue(TicketStateContext context) { /* Invalid */ }
        public void CheckIn(TicketStateContext context) { /* Invalid */ }
        public void Board(TicketStateContext context) { /* Invalid */ }
        public void Cancel(TicketStateContext context) => context.ChangeState(new CancelledState());
        public void Refund(TicketStateContext context) { /* Invalid, no money paid yet */ }
    }

    public class PaymentProcessingState : ITicketState
    {
        public void Pay(TicketStateContext context) { /* Invalid */ }
        public void Issue(TicketStateContext context) => context.ChangeState(new IssuedState());
        public void CheckIn(TicketStateContext context) { /* Invalid */ }
        public void Board(TicketStateContext context) { /* Invalid */ }
        public void Cancel(TicketStateContext context) => context.ChangeState(new CancelledState());
        public void Refund(TicketStateContext context) { /* Wait for process to finish before refunding */ }
    }

    public class IssuedState : ITicketState
    {
        public void Pay(TicketStateContext context) { /* Invalid */ }
        public void Issue(TicketStateContext context) { /* Invalid */ }
        public void CheckIn(TicketStateContext context) => context.ChangeState(new CheckedInState());
        public void Board(TicketStateContext context) { /* Invalid */ }
        public void Cancel(TicketStateContext context) => context.ChangeState(new CancelledState());
        public void Refund(TicketStateContext context) { /* Invalid */ }
    }

    public class CheckedInState : ITicketState
    {
        public void Pay(TicketStateContext context) { /* Invalid */ }
        public void Issue(TicketStateContext context) { /* Invalid */ }
        public void CheckIn(TicketStateContext context) { /* Invalid */ }
        public void Board(TicketStateContext context) => context.ChangeState(new BoardedState());
        public void Cancel(TicketStateContext context) => context.ChangeState(new CancelledState()); // Can offload passenger
        public void Refund(TicketStateContext context) { /* Invalid */ }
    }

    public class BoardedState : ITicketState
    {
        public void Pay(TicketStateContext context) { /* Invalid */ }
        public void Issue(TicketStateContext context) { /* Invalid */ }
        public void CheckIn(TicketStateContext context) { /* Invalid */ }
        public void Board(TicketStateContext context) { /* Invalid */ }
        public void Cancel(TicketStateContext context) { /* Invalid */ } // Already on plane
        public void Refund(TicketStateContext context) { /* Invalid */ }
    }

    public class CancelledState : ITicketState
    {
        public void Pay(TicketStateContext context) { /* Invalid */ }
        public void Issue(TicketStateContext context) { /* Invalid */ }
        public void CheckIn(TicketStateContext context) { /* Invalid */ }
        public void Board(TicketStateContext context) { /* Invalid */ }
        public void Cancel(TicketStateContext context) { /* Invalid */ }
        public void Refund(TicketStateContext context) => context.ChangeState(new RefundedState());
    }

    public class RefundedState : ITicketState
    {
        public void Pay(TicketStateContext context) { /* Invalid */ }
        public void Issue(TicketStateContext context) { /* Invalid */ }
        public void CheckIn(TicketStateContext context) { /* Invalid */ }
        public void Board(TicketStateContext context) { /* Invalid */ }
        public void Cancel(TicketStateContext context) { /* Invalid */ }
        public void Refund(TicketStateContext context) { /* Invalid, already refunded */ }
    }

    public class TicketMachine
    {
        public ITicketMachineState State { get; set; }
        public int Balance { get; set; }
        public int TicketPrice { get; } = 50;
        public List<string> Logs { get; } = new List<string>();

        public TicketMachine()
        {
            // Initial state
            State = new WaitingForMoneyState();
            Balance = 0;
            Logs.Add("Machine initialized. State: WaitingForMoney.");
        }

        public void InsertMoney(int amount)
        {
            State.InsertMoney(this, amount);
        }

        public void RequestTicket()
        {
            State.RequestTicket(this);
        }

        public void Dispense()
        {
            State.Dispense(this);
        }
    }

    // 2. State Interface
    public interface ITicketMachineState
    {
        void InsertMoney(TicketMachine machine, int amount);
        void RequestTicket(TicketMachine machine);
        void Dispense(TicketMachine machine);
    }

    // 3. Concrete States
    public class WaitingForMoneyState : ITicketMachineState
    {
        public void InsertMoney(TicketMachine machine, int amount)
        {
            machine.Balance += amount;
            machine.Logs.Add($"Inserted {amount} USD. Total: {machine.Balance} USD.");
            
            if (machine.Balance >= machine.TicketPrice)
            {
                machine.State = new ValidatingPaymentState();
                machine.Logs.Add("State changed to: ValidatingPayment.");
            }
        }

        public void RequestTicket(TicketMachine machine)
        {
            machine.Logs.Add($"❌ Cannot issue ticket. Need {machine.TicketPrice - machine.Balance} USD more.");
        }

        public void Dispense(TicketMachine machine)
        {
            machine.Logs.Add("❌ Cannot dispense. Insert money first.");
        }
    }

    public class ValidatingPaymentState : ITicketMachineState
    {
        public void InsertMoney(TicketMachine machine, int amount)
        {
            machine.Balance += amount;
            machine.Logs.Add($"Inserted {amount} USD. Total: {machine.Balance} USD.");
        }

        public void RequestTicket(TicketMachine machine)
        {
            machine.Logs.Add("✅ Payment validated. Processing ticket request...");
            machine.State = new IssuingTicketState();
            machine.Logs.Add("State changed to: IssuingTicket.");
        }

        public void Dispense(TicketMachine machine)
        {
            machine.Logs.Add("❌ Cannot dispense yet. Please request ticket first.");
        }
    }

    public class IssuingTicketState : ITicketMachineState
    {
        public void InsertMoney(TicketMachine machine, int amount)
        {
            machine.Logs.Add("❌ Please wait, currently issuing a ticket. Returning money.");
        }

        public void RequestTicket(TicketMachine machine)
        {
            machine.Logs.Add("❌ Already processing your ticket. Please wait.");
        }

        public void Dispense(TicketMachine machine)
        {
            int change = machine.Balance - machine.TicketPrice;
            machine.Logs.Add($"🎫 Ticket Dispensed! Change returned: {change} USD.");
            
            // Reset machine
            machine.Balance = 0;
            machine.State = new WaitingForMoneyState();
            machine.Logs.Add("Machine reset. State changed to: WaitingForMoney.");
        }
    }
}
