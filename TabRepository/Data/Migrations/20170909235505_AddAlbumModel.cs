using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TabRepository.Data.Migrations
{
    public partial class AddAlbumModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tabs_Projects_ProjectId",
                table: "Tabs");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "Tabs",
                newName: "AlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_Tabs_ProjectId",
                table: "Tabs",
                newName: "IX_Tabs_AlbumId");

            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CurrentVersion = table.Column<int>(nullable: false),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    DateModified = table.Column<DateTime>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    ImageFileName = table.Column<string>(nullable: true),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    ProjectId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Albums_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Albums_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Albums_ProjectId",
                table: "Albums",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_UserId",
                table: "Albums",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tabs_Albums_AlbumId",
                table: "Tabs",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tabs_Albums_AlbumId",
                table: "Tabs");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.RenameColumn(
                name: "AlbumId",
                table: "Tabs",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Tabs_AlbumId",
                table: "Tabs",
                newName: "IX_Tabs_ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tabs_Projects_ProjectId",
                table: "Tabs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
