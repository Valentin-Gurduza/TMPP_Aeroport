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
        public string RenderHeader(string title) => $"<div class='text-center border-b border-red-500/30 pb-3 mb-4 font-bold text-red-400'>[LED MATRIX] ---- {title.ToUpper()} ----</div>";
        
        public string RenderRow(Flight flight) 
        {
            return $"<div class='flex items-center gap-4 bg-red-900/10 p-2 rounded border border-red-500/10 text-red-500 font-mono text-sm'>" +
                   $"<span class='text-xs opacity-60 w-12'>{flight.DepartureTime:HH:mm}</span>" +
                   $"<span class='flex-1 font-bold w-32'>&gt; {flight.FlightNumber}</span>" +
                   $"<span class='flex-1 opacity-80'>{flight.Destination}</span>" +
                   $"<span class='bg-red-500/20 px-2 py-0.5 rounded text-xs font-bold border border-red-500/30 animate-pulse w-24 text-center'>{flight.Status.ToUpper()}</span>" +
                   $"</div>";
        }
        
        public string RenderFooter() => $"<div class='text-center border-t border-red-500/30 pt-2 mt-4 text-xs opacity-50'>[END OF LED DATA]</div>";
    }

    // Concrete Implementation 2: Monitor Smart TV LCD
    public class WebRenderer : IDisplayRenderer
    {
        public string RenderHeader(string title) => $"<div class='text-center border-b border-cyan-500/30 pb-3 mb-4 font-bold text-cyan-300'>✈ {title.ToUpper()} (SMART WEB)</div>";
        
        public string RenderRow(Flight flight) 
        {
            string statusColor = flight.Status == "Airborne" ? "text-emerald-300 bg-emerald-500/20 border-emerald-500/30" : 
                                 flight.Status == "Boarding" ? "text-amber-300 bg-amber-500/20 border-amber-500/30 animate-pulse" : 
                                 "text-cyan-300 bg-cyan-500/20 border-cyan-500/30";
            
            return $"<div class='flex items-center gap-4 bg-cyan-900/20 p-2 rounded border border-cyan-500/10'>" +
                   $"<span class='text-xs opacity-60 w-12 text-cyan-200'>{flight.DepartureTime:HH:mm}</span>" +
                   $"<span class='flex-1 font-bold text-white w-32'>&gt; {flight.FlightNumber}</span>" +
                   $"<span class='flex-1 text-cyan-100'>{flight.Destination}</span>" +
                   $"<span class='px-2 py-0.5 rounded text-xs font-bold border {statusColor} w-24 text-center'>{flight.Status.ToUpper()}</span>" +
                   $"</div>";
        }
        
        public string RenderFooter() => $"<div class='text-center pt-2 mt-4 text-xs text-cyan-600 font-mono'>Powered by WebRenderer UI</div>";
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
            var output = renderer.RenderHeader("Sosiri Curente (Arrivals)");
            foreach (var f in flights)
            {
                output += "\n" + renderer.RenderRow(f);
            }
            output += "\n" + renderer.RenderFooter();
            return output;
        }
    }
}
