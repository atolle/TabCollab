using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class AddAccountTypeLocked : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccountTypeLocked",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountTypeLocked",
                table: "AspNetUsers");
        }
    }
}
