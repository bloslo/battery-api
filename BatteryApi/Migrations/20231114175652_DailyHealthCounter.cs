using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BatteryApi.Migrations
{
    /// <inheritdoc />
    public partial class DailyHealthCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyHealthCounter",
                table: "Batteries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyHealthCounter",
                table: "Batteries");
        }
    }
}
