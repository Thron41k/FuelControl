using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FuelingRecorUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fueling_uss_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FuelingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    OmnicommEventId = table.Column<int>(type: "integer", nullable: false),
                    OmnicommReportId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OmnicommFuelTruckId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VolumeLiters = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fueling_uss_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fueling_uss_records_fueling_records_FuelingRecordId",
                        column: x => x.FuelingRecordId,
                        principalTable: "fueling_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fueling_uss_records_FuelingRecordId",
                table: "fueling_uss_records",
                column: "FuelingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_fueling_uss_records_OmnicommEventId",
                table: "fueling_uss_records",
                column: "OmnicommEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fueling_uss_records_OmnicommFuelTruckId",
                table: "fueling_uss_records",
                column: "OmnicommFuelTruckId");

            migrationBuilder.CreateIndex(
                name: "IX_fueling_uss_records_OmnicommFuelTruckId_StartDate",
                table: "fueling_uss_records",
                columns: new[] { "OmnicommFuelTruckId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_fueling_uss_records_OmnicommReportId",
                table: "fueling_uss_records",
                column: "OmnicommReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fueling_uss_records");
        }
    }
}
