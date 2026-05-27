using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPP_Aeroport.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightOriginAndTicketUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_FlightId",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Flights",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_FlightId_SeatNumber",
                table: "Tickets",
                columns: new[] { "FlightId", "SeatNumber" },
                unique: true,
                filter: "[TicketState] <> 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_FlightId_SeatNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Flights");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_FlightId",
                table: "Tickets",
                column: "FlightId");
        }
    }
}
