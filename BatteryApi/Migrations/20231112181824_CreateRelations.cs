using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BatteryApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatteryId",
                table: "BatteryIssues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BatteryIssues_BatteryId",
                table: "BatteryIssues",
                column: "BatteryId");

            migrationBuilder.AddForeignKey(
                name: "FK_BatteryIssues_Batteries_BatteryId",
                table: "BatteryIssues",
                column: "BatteryId",
                principalTable: "Batteries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatteryIssues_Batteries_BatteryId",
                table: "BatteryIssues");

            migrationBuilder.DropIndex(
                name: "IX_BatteryIssues_BatteryId",
                table: "BatteryIssues");

            migrationBuilder.DropColumn(
                name: "BatteryId",
                table: "BatteryIssues");
        }
    }
}
