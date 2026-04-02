using System;
using System.Collections.Generic;

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
        string RenderRow(string flightInfo);
        string RenderFooter();
    }

    // Concrete Implementation 1: Panou Tradițional LED în Terminal
    public class LEDRenderer : IDisplayRenderer
    {
        public string RenderHeader(string title) => $"[LED MATRIX] ---- {title.ToUpper()} ----";
        public string RenderRow(string info) => $"[LED RED] > {info}";
        public string RenderFooter() => $"[LED MATRIX] ------------------------\n";
    }

    // Concrete Implementation 2: Monitor Smart TV LCD
    public class WebRenderer : IDisplayRenderer
    {
        public string RenderHeader(string title) => $"<h3>🌐 {title}</h3>\n<ul>";
        public string RenderRow(string info) => $"   <li>{info}</li>";
        public string RenderFooter() => $"</ul>\n";
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

        public abstract string ShowBoard(List<string> flights);
    }

    // Refined Abstraction 1: Tabel pentru Plecări
    public class DeparturesBoard : FlightBoard
    {
        public DeparturesBoard(IDisplayRenderer renderer) : base(renderer) { }

        public override string ShowBoard(List<string> flights)
        {
            var output = renderer.RenderHeader("Plecări Imediat (Departures)");
            foreach (var f in flights)
            {
                // Aici aplicăm o scurtă logică de business specifică 'Plecărilor'
                output += "\n" + renderer.RenderRow($"{f} - TAKEOFF READY");
            }
            output += "\n" + renderer.RenderFooter();
            return output;
        }
    }

    // Refined Abstraction 2: Tabel pentru Sosiri
    public class ArrivalsBoard : FlightBoard
    {
        public ArrivalsBoard(IDisplayRenderer renderer) : base(renderer) { }

        public override string ShowBoard(List<string> flights)
        {
            var output = renderer.RenderHeader("Sosiri Curente (Arrivals)");
            foreach (var f in flights)
            {
                output += "\n" + renderer.RenderRow($"{f} - LANDED / BAGGAGE GATE 4");
            }
            output += "\n" + renderer.RenderFooter();
            return output;
        }
    }
}
