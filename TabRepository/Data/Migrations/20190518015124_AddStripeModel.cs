using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace TabRepository.Migrations
{
    public partial class AddStripeModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StripeProducts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StripePlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StripePlans_StripeProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "StripeProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StripeSubscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StripeSubscriptions_StripePlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "StripePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StripeCustomers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubscriptionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeCustomers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StripeCustomers_StripeSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "StripeSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StripeCustomers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StripeCustomers_SubscriptionId",
                table: "StripeCustomers",
                column: "SubscriptionId",
                unique: true,
                filter: "[SubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StripeCustomers_UserId",
                table: "StripeCustomers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StripePlans_ProductId",
                table: "StripePlans",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeSubscriptions_PlanId",
                table: "StripeSubscriptions",
                column: "PlanId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeCustomers");

            migrationBuilder.DropTable(
                name: "StripeSubscriptions");

            migrationBuilder.DropTable(
                name: "StripePlans");

            migrationBuilder.DropTable(
                name: "StripeProducts");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AspNetUsers");
        }
    }
}
