using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniStravaBackend.Migrations
{
    /// <inheritdoc />
    public partial class UserStatsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FastestPace",
                table: "UserStats",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxDistance",
                table: "UserStats",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalDuration",
                table: "UserStats",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FastestPace",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "MaxDistance",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "TotalDuration",
                table: "UserStats");
        }
    }
}
