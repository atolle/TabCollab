using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class AddSecondMessageToNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Message",
                table: "Notifications",
                newName: "Message2");

            migrationBuilder.AddColumn<string>(
                name: "Message1",
                table: "Notifications",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message1",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "Message2",
                table: "Notifications",
                newName: "Message");
        }
    }
}
