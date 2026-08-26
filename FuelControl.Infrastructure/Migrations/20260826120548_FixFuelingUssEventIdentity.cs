using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFuelingUssEventIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_fueling_uss_records_OmnicommReportId_OmnicommEventId",
                table: "fueling_uss_records",
                columns: new[] { "OmnicommReportId", "OmnicommEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fueling_uss_records_OmnicommReportId_OmnicommEventId",
                table: "fueling_uss_records");
        }
    }
}
