using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class AddIntervalToPlan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Interval",
                table: "StripePlans",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Interval",
                table: "StripePlans");
        }
    }
}
