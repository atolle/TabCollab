using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class AddTabTutorialMobile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TabTutorialMobileShown",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TabTutorialMobileShown",
                table: "AspNetUsers");
        }
    }
}
