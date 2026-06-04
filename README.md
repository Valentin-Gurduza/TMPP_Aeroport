# Smart Airport Management System (SAMS) ✈️

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue)
![TailwindCSS](https://img.shields.io/badge/Tailwind-CSS-38B2AC?logo=tailwind-css)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoft-sql-server)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-0078D4)

Smart Airport Management System (SAMS) is an enterprise-grade ASP.NET Core MVC application developed as a laboratory project to demonstrate software engineering principles, Design Patterns, and modern web application development.

## 🌟 Key Features

* **Enterprise UI / UX:** Fully responsive, modern corporate dashboard designed with Tailwind CSS.
* **Role-Based Access Control (RBAC):** Built-in Identity system with roles for `Admin`, `ATC_Manager`, `Ground_Staff`, and `Passenger`.
* **Real-time FIDS Board:** Flight Information Display System updated in real-time across all clients using **SignalR** WebSockets.
* **Flight & Fleet Management:** Complete CRUD functionality for Aircraft and Flights.
* **Ticket Purchasing Flow:** Passenger ticket booking with state-based validation (State Pattern).
* **Design Patterns Laboratory:** Practical implementation of GoF Design Patterns:
  * *Creational:* Abstract Factory, Builder, Factory Method, Prototype, Singleton
  * *Structural:* Adapter, Bridge, Composite, Decorator, Facade, Flyweight, Proxy
  * *Behavioral:* Chain of Responsibility, Command, Interpreter, Iterator, Mediator, Memento, Observer, State, Strategy, Template Method, Visitor
* **Production-Ready Polish:** Custom HTTP error pages (404/403/500), client-side DataAnnotations validation, database seeding on startup, and SEO metadata.

## 🚀 Getting Started

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
* SQL Server (Express/Developer)
* Visual Studio 2022 or VS Code

### Installation & Setup

1. **Clone the repository or extract the project folder:**
   Ensure the solution file is opened in your IDE.

2. **Database Setup:**
   The project uses Entity Framework Core to manage the database schema. The connection string in `appsettings.json` is configured for a default SQL Server instance (`Server=.;Database=SAMS_Db;...`). If you are using SQL Express, you might need to update it to `Server=.\SQLEXPRESS`.

   Open the Package Manager Console (PMC) or terminal in the project directory and run:
   ```bash
   dotnet ef database update
   ```
   *This command will create the database and apply all necessary tables.*

3. **Run the Application:**
   ```bash
   dotnet run
   ```
   Or simply hit `F5` in Visual Studio to start debugging.

4. **Automatic Seed Data:**
   When the application starts for the first time, it will automatically populate the database with:
   * **System Roles:** `Admin`, `ATC_Manager`, `Ground_Staff`, `Passenger`
   * **Admin User:** Email: `admin@sams.local` / Password: `Admin123!`
   * **Sample Data:** 3 Aircrafts and 5 scheduled flights to instantly populate the FIDS Board.

## 🔐 Accounts for Testing

Use the following credentials to explore different areas of the system:

* **Administrator:**
  * **Email:** `admin@sams.local`
  * **Password:** `Admin123!`
  * *Access:* Full system access, including User & Role Management.

*(You can create new users via the Register page and then assign them specific roles via the Admin panel `User Mgmt`).*

## 📁 Project Structure

* **`/Controllers`**: Handles HTTP requests. Notable controllers include `AirportController` (Core logic), `AdminController` (Identity Management), `FlightsController`, `FleetController`, and `TicketsController`.
* **`/Domain`**: The heart of the laboratory project. Contains folders for every implemented Design Pattern (`AbstractFactory`, `Proxy`, `Strategy`, `Mediator`, etc.).
* **`/Views`**: Razor Views styled with Tailwind CSS. Includes specialized views like `_LayoutCorporate.cshtml` (Layout) and custom error pages in `/Views/Home/`.
* **`/Hubs`**: Contains `FlightHub.cs` for SignalR real-time communications.
* **`/Data`**: Entity Framework `ApplicationDbContext` and `DbSeeder.cs` for startup initialization.
* **`/Models`**: Core entities like `ApplicationUser`, `Aircraft`, `Flight`, and `Ticket`.

## 🛠️ Built With
* [ASP.NET Core MVC 10.0](https://docs.microsoft.com/en-us/aspnet/core/) - Web Framework
* [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - ORM for Database Management
* [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity) - Authentication & Authorization
* [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr) - Real-time Web Functionality
* [Tailwind CSS](https://tailwindcss.com/) - Utility-first CSS framework (via CDN)
* [Google Material Symbols](https://fonts.google.com/icons) - Modern Iconography

---
