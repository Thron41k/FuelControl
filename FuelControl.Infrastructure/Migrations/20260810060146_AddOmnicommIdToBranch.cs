using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOmnicommIdToBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OmnicommId",
                table: "Branches",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_OmnicommId",
                table: "Branches",
                column: "OmnicommId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Branches_OmnicommId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "OmnicommId",
                table: "Branches");
        }
    }
}
