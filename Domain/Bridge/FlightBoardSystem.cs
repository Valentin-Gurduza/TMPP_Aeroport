using System;
using System.Collections.Generic;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Domain.Bridge
{
    // =========================================================
    // BRIDGE IMPLEMENTATION
    // =========================================================
    // Modul independent în care sunt afișate informațiile fizic
    // (Afișajul hardware, platforma web, panoul cu leduri analogice).
    public interface IDisplayRenderer
    {
        string RenderHeader(string title);
        string RenderRow(Flight flight);
        string RenderFooter();
    }

    // Concrete Implementation 1: Panou Tradițional LED în Terminal
    public class LEDRenderer : IDisplayRenderer
    {
        public string RenderHeader(string title) => $"[LED MATRIX] ---- {title.ToUpper()} ----";
        public string RenderRow(Flight flight) => $"[LED RED] > {flight.FlightNumber} | {flight.Destination} | {flight.DepartureTime:HH:mm} | {flight.Status.ToUpper()}";
        public string RenderFooter() => $"[LED MATRIX] ------------------------\n";
    }

    // Concrete Implementation 2: Monitor Smart TV LCD
    public class WebRenderer : IDisplayRenderer
    {
        public string RenderHeader(string title) => $"<div class='bg-slate-800 text-white rounded-xl overflow-hidden shadow-lg border border-slate-700'><div class='bg-slate-900 px-4 py-3 border-b border-slate-700 font-bold uppercase tracking-wider text-sm flex items-center gap-2'><span class='material-symbols-outlined text-blue-400'>monitor</span> {title}</div><div class='divide-y divide-slate-700'>";
        
        public string RenderRow(Flight flight) 
        {
            string statusColor = flight.Status == "Airborne" ? "text-emerald-400" : 
                                 flight.Status == "Boarding" ? "text-amber-400" : "text-slate-300";
            
            return $@"
                <div class='px-4 py-3 flex justify-between items-center hover:bg-slate-750 transition'>
                    <div class='flex flex-col'>
                        <span class='font-bold text-slate-100'>{flight.FlightNumber}</span>
                        <span class='text-xs text-slate-400'>{flight.Destination}</span>
                    </div>
                    <div class='flex flex-col items-end'>
                        <span class='font-mono text-sm text-blue-300'>{flight.DepartureTime:HH:mm}</span>
                        <span class='text-xs font-bold uppercase {statusColor}'>{flight.Status}</span>
                    </div>
                </div>";
        }
        
        public string RenderFooter() => $"</div></div>\n";
    }

    // =========================================================
    // BRIDGE ABSTRACTION
    // =========================================================
    // Partea de business (Ce anume arătăm pasagerilor).
    // Conține referința spre IDisplayRenderer (Podul - The Bridge).
    // Astfel putem schimba hardware-ul menținând logica zborurilor.
    public abstract class FlightBoard
    {
        protected IDisplayRenderer renderer; // Podul de legătură

        protected FlightBoard(IDisplayRenderer renderer)
        {
            this.renderer = renderer;
        }

        public abstract string ShowBoard(IEnumerable<Flight> flights);
    }

    // Refined Abstraction 1: Tabel pentru Plecări
    public class DeparturesBoard : FlightBoard
    {
        public DeparturesBoard(IDisplayRenderer renderer) : base(renderer) { }

        public override string ShowBoard(IEnumerable<Flight> flights)
        {
            var output = renderer.RenderHeader("Plecări Curente (Departures)");
            foreach (var f in flights)
            {
                output += "\n" + renderer.RenderRow(f);
            }
            output += "\n" + renderer.RenderFooter();
            return output;
        }
    }

    // Refined Abstraction 2: Tabel pentru Sosiri
    public class ArrivalsBoard : FlightBoard
    {
        public ArrivalsBoard(IDisplayRenderer renderer) : base(renderer) { }

        public override string ShowBoard(IEnumerable<Flight> flights)
        {
            var output = renderer.RenderHeader("Afișaj Secundar (Hardware Test)");
            foreach (var f in flights)
            {
                output += "\n" + renderer.RenderRow(f);
            }
            output += "\n" + renderer.RenderFooter();
            return output;
        }
    }
}
