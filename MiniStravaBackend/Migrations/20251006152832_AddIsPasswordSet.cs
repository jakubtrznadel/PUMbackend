using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniStravaBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPasswordSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPasswordSet",
                table: "Admins",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPasswordSet",
                table: "Admins");
        }
    }
}
