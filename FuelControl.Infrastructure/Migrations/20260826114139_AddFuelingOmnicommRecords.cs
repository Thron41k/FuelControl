using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFuelingOmnicommRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fueling_omnicomm_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FuelingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    OmnicommEventId = table.Column<int>(type: "integer", nullable: false),
                    OmnicommReportId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OmnicommVehicleId = table.Column<long>(type: "bigint", nullable: false),
                    VehicleName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VolumeLiters = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    MatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MatchedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fueling_omnicomm_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fueling_omnicomm_records_fueling_records_FuelingRecordId",
                        column: x => x.FuelingRecordId,
                        principalTable: "fueling_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fueling_omnicomm_records_EndDate",
                table: "fueling_omnicomm_records",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_fueling_omnicomm_records_FuelingRecordId",
                table: "fueling_omnicomm_records",
                column: "FuelingRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fueling_omnicomm_records_OmnicommReportId_OmnicommEventId",
                table: "fueling_omnicomm_records",
                columns: new[] { "OmnicommReportId", "OmnicommEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fueling_omnicomm_records_OmnicommVehicleId",
                table: "fueling_omnicomm_records",
                column: "OmnicommVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_fueling_omnicomm_records_StartDate",
                table: "fueling_omnicomm_records",
                column: "StartDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fueling_omnicomm_records");
        }
    }
}
