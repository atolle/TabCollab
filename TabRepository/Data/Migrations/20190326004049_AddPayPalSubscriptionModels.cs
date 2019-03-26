using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace TabRepository.Migrations
{
    public partial class AddPayPalSubscriptionModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayPalBillingPlan",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalBillingPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayPalProducts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayPalBillingAgreement",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BillingAgreementId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecuteURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RequestToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalBillingAgreement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayPalBillingAgreement_PayPalBillingPlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "PayPalBillingPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayPalBillingAgreement_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayPalPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayPalPlans_PayPalProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PayPalProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayPalSubscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubscriptionToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayPalSubscriptions_PayPalPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "PayPalPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayPalSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayPalBillingAgreement_PlanId",
                table: "PayPalBillingAgreement",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalBillingAgreement_UserId",
                table: "PayPalBillingAgreement",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalPlans_ProductId",
                table: "PayPalPlans",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalSubscriptions_PlanId",
                table: "PayPalSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalSubscriptions_UserId",
                table: "PayPalSubscriptions",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayPalBillingAgreement");

            migrationBuilder.DropTable(
                name: "PayPalSubscriptions");

            migrationBuilder.DropTable(
                name: "PayPalBillingPlan");

            migrationBuilder.DropTable(
                name: "PayPalPlans");

            migrationBuilder.DropTable(
                name: "PayPalProducts");
        }
    }
}
