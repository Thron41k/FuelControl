using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFuelTruckOmnicommBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fuel_truck_omnicomm_bindings");

            migrationBuilder.AddColumn<Guid>(
                name: "TankVehicleId",
                table: "fuel_trucks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UssVehicleId",
                table: "fuel_trucks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fuel_trucks_TankVehicleId",
                table: "fuel_trucks",
                column: "TankVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_trucks_UssVehicleId",
                table: "fuel_trucks",
                column: "UssVehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_fuel_trucks_vehicles_TankVehicleId",
                table: "fuel_trucks",
                column: "TankVehicleId",
                principalTable: "vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fuel_trucks_vehicles_UssVehicleId",
                table: "fuel_trucks",
                column: "UssVehicleId",
                principalTable: "vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fuel_trucks_vehicles_TankVehicleId",
                table: "fuel_trucks");

            migrationBuilder.DropForeignKey(
                name: "FK_fuel_trucks_vehicles_UssVehicleId",
                table: "fuel_trucks");

            migrationBuilder.DropIndex(
                name: "IX_fuel_trucks_TankVehicleId",
                table: "fuel_trucks");

            migrationBuilder.DropIndex(
                name: "IX_fuel_trucks_UssVehicleId",
                table: "fuel_trucks");

            migrationBuilder.DropColumn(
                name: "TankVehicleId",
                table: "fuel_trucks");

            migrationBuilder.DropColumn(
                name: "UssVehicleId",
                table: "fuel_trucks");

            migrationBuilder.CreateTable(
                name: "fuel_truck_omnicomm_bindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FuelTruckId = table.Column<Guid>(type: "uuid", nullable: false),
                    OmnicommObjectId = table.Column<long>(type: "bigint", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_truck_omnicomm_bindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fuel_truck_omnicomm_bindings_fuel_trucks_FuelTruckId",
                        column: x => x.FuelTruckId,
                        principalTable: "fuel_trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fuel_truck_omnicomm_bindings_FuelTruckId_Purpose",
                table: "fuel_truck_omnicomm_bindings",
                columns: new[] { "FuelTruckId", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fuel_truck_omnicomm_bindings_OmnicommObjectId",
                table: "fuel_truck_omnicomm_bindings",
                column: "OmnicommObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_truck_omnicomm_bindings_OmnicommObjectId_Purpose",
                table: "fuel_truck_omnicomm_bindings",
                columns: new[] { "OmnicommObjectId", "Purpose" },
                unique: true);
        }
    }
}
