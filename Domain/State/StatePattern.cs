using System;
using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.State
{
    // 1. Context Class
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
