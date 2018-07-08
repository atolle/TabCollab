using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace TabRepository.Data.Migrations
{
    public partial class AddUserTabVersionModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Projects_ProjectId",
                table: "Notifications");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Notifications",
                type: "int4",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.CreateTable(
                name: "UserTabVersions",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TabId = table.Column<int>(type: "int4", nullable: false),
                    Version = table.Column<int>(type: "int4", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTabVersions", x => new { x.UserId, x.TabId });
                    table.ForeignKey(
                        name: "FK_UserTabVersions_Tabs_TabId",
                        column: x => x.TabId,
                        principalTable: "Tabs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTabVersions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTabVersions_TabId",
                table: "UserTabVersions",
                column: "TabId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Projects_ProjectId",
                table: "Notifications",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Projects_ProjectId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "UserTabVersions");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Notifications",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int4",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Projects_ProjectId",
                table: "Notifications",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
