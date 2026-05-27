using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMPP_Aeroport.Domain.Interfaces;
using System.Collections.Generic;
using System;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManagementController : Controller
    {
        private readonly IFlightService _flightService;

        public ManagementController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        // Prototype & Abstract Factory: Flight Scheduler
        public IActionResult FlightScheduler(Guid? cloneFlightId, string newTicketType)
        {
            if (cloneFlightId.HasValue)
            {
                // Prototype Pattern: Clone existing flight
                _flightService.CloneFlight(cloneFlightId.Value);
            }

            if (!string.IsNullOrEmpty(newTicketType))
            {
                // Abstract Factory Pattern
                TMPP_Aeroport.Domain.AbstractFactory.IFlightDocumentFactory factory;
                if (newTicketType.ToLower() == "business")
                    factory = new TMPP_Aeroport.Domain.AbstractFactory.BusinessDocumentFactory();
                else
                    factory = new TMPP_Aeroport.Domain.AbstractFactory.EconomyDocumentFactory();

                var boardingPass = factory.CreateBoardingPass();
                var baggageTag = factory.CreateBaggageTag();
                
                boardingPass.PassengerName = "Sample Passenger";
                boardingPass.FlightNumber = "GENERIC-001";
                baggageTag.Code = "BGG-SAMPLE";

                ViewBag.SampleBoardingPass = boardingPass;
                ViewBag.SampleBaggageTag = baggageTag;
            }

            var flights = _flightService.GetAllFlights();
            return View(flights);
        }

        // Singleton: System Audit Logs
        public IActionResult AuditLogs()
        {
            var logs = TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.GetLogs();
            return View(logs);
        }

        // Visitor: Analytics & Export
        public IActionResult Analytics(string format = "json")
        {
            var elements = new List<TMPP_Aeroport.Domain.Visitor.IAirportElement>
            {
                new TMPP_Aeroport.Domain.Visitor.TerminalElement("Terminal 1", 15),
                new TMPP_Aeroport.Domain.Visitor.AircraftElement("Boeing 737 MAX", 180),
                new TMPP_Aeroport.Domain.Visitor.FlightElement("RO-302", "Frankfurt")
            };

            TMPP_Aeroport.Domain.Visitor.IVisitor visitor;
            if (format == "xml") visitor = new TMPP_Aeroport.Domain.Visitor.XmlExportVisitor();
            else visitor = new TMPP_Aeroport.Domain.Visitor.JsonExportVisitor();

            foreach (var element in elements)
            {
                element.Accept(visitor);
            }

            if (format == "xml")
                ViewBag.ExportData = ((TMPP_Aeroport.Domain.Visitor.XmlExportVisitor)visitor).ExportedData;
            else
                ViewBag.ExportData = ((TMPP_Aeroport.Domain.Visitor.JsonExportVisitor)visitor).ExportedData;

            ViewBag.Format = format;
            return View();
        }

        public IActionResult DownloadAnalytics(string format = "json")
        {
            var elements = new List<TMPP_Aeroport.Domain.Visitor.IAirportElement>
            {
                new TMPP_Aeroport.Domain.Visitor.TerminalElement("Terminal 1", 15),
                new TMPP_Aeroport.Domain.Visitor.AircraftElement("Boeing 737 MAX", 180),
                new TMPP_Aeroport.Domain.Visitor.FlightElement("RO-302", "Frankfurt")
            };

            string exportData = "";
            string contentType = "application/json";
            string fileName = "airport_export.json";

            if (format == "xml")
            {
                var visitor = new TMPP_Aeroport.Domain.Visitor.XmlExportVisitor();
                foreach (var element in elements) element.Accept(visitor);
                exportData = string.Join(Environment.NewLine, visitor.ExportedData);
                contentType = "application/xml";
                fileName = "airport_export.xml";
            }
            else
            {
                var visitor = new TMPP_Aeroport.Domain.Visitor.JsonExportVisitor();
                foreach (var element in elements) element.Accept(visitor);
                exportData = string.Join(Environment.NewLine, visitor.ExportedData);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(exportData);
            return File(bytes, contentType, fileName);
        }
    }
}
