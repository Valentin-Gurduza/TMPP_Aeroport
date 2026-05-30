using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPP_Aeroport.Migrations
{
    /// <inheritdoc />
    public partial class V4_ExtendedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaggageStage",
                table: "BaggageItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StageUpdatedAt",
                table: "BaggageItems",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaggageStage",
                table: "BaggageItems");

            migrationBuilder.DropColumn(
                name: "StageUpdatedAt",
                table: "BaggageItems");
        }
    }
}
