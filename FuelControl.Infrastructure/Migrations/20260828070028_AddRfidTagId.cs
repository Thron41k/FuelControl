using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRfidTagId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RfidTagId",
                table: "vehicles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidTagId",
                table: "operators",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_RfidTagId",
                table: "vehicles",
                column: "RfidTagId",
                unique: true,
                filter: "\"RfidTagId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_operators_RfidTagId",
                table: "operators",
                column: "RfidTagId",
                unique: true,
                filter: "\"RfidTagId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vehicles_RfidTagId",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_operators_RfidTagId",
                table: "operators");

            migrationBuilder.DropColumn(
                name: "RfidTagId",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "RfidTagId",
                table: "operators");
        }
    }
}
