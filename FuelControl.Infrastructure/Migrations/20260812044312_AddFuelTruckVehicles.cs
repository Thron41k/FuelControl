using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFuelTruckVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fuel_trucks_Branches_BranchId",
                table: "fuel_trucks");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_OmnicommObjectId",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_fuel_trucks_BranchId",
                table: "fuel_trucks");

            migrationBuilder.DropIndex(
                name: "IX_fuel_trucks_OmnicommObjectId",
                table: "fuel_trucks");

            migrationBuilder.DropIndex(
                name: "IX_fuel_trucks_RegistrationNumber",
                table: "fuel_trucks");

            migrationBuilder.DropColumn(
                name: "InventoryNumber",
                table: "fuel_trucks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "fuel_trucks");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "fuel_trucks");

            migrationBuilder.DropColumn(
                name: "OmnicommObjectId",
                table: "fuel_trucks");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "fuel_trucks");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "fuel_trucks",
                newName: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_OmnicommObjectId",
                table: "vehicles",
                column: "OmnicommObjectId",
                unique: true,
                filter: "\"OmnicommObjectId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_trucks_VehicleId",
                table: "fuel_trucks",
                column: "VehicleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_fuel_trucks_vehicles_VehicleId",
                table: "fuel_trucks",
                column: "VehicleId",
                principalTable: "vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fuel_trucks_vehicles_VehicleId",
                table: "fuel_trucks");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_OmnicommObjectId",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_fuel_trucks_VehicleId",
                table: "fuel_trucks");

            migrationBuilder.RenameColumn(
                name: "VehicleId",
                table: "fuel_trucks",
                newName: "BranchId");

            migrationBuilder.AddColumn<string>(
                name: "InventoryNumber",
                table: "fuel_trucks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "fuel_trucks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "fuel_trucks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "OmnicommObjectId",
                table: "fuel_trucks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "fuel_trucks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_OmnicommObjectId",
                table: "vehicles",
                column: "OmnicommObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_trucks_BranchId",
                table: "fuel_trucks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_trucks_OmnicommObjectId",
                table: "fuel_trucks",
                column: "OmnicommObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_trucks_RegistrationNumber",
                table: "fuel_trucks",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_fuel_trucks_Branches_BranchId",
                table: "fuel_trucks",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
