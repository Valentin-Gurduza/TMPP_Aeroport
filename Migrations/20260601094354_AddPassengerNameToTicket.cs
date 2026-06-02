using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPP_Aeroport.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerNameToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PassengerName",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PassengerName",
                table: "Tickets");
        }
    }
}
