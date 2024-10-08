﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class AddStripeInvoice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "StripeSubscriptions");

            migrationBuilder.CreateTable(
                name: "StripeInvoices",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    SubscriptionId = table.Column<string>(nullable: true),
                    CustomerId = table.Column<string>(nullable: true),
                    ChargeId = table.Column<string>(nullable: true),
                    Subtotal = table.Column<double>(nullable: false),
                    Tax = table.Column<double>(nullable: false),
                    ReceiptURL = table.Column<string>(nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    DateDue = table.Column<DateTime>(nullable: true),
                    DatePaid = table.Column<DateTime>(nullable: true),
                    PaymentStatus = table.Column<int>(nullable: false),
                    PaymentStatusText = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StripeInvoices_StripeCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "StripeCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StripeInvoices_StripeSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "StripeSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StripeInvoices_CustomerId",
                table: "StripeInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeInvoices_SubscriptionId",
                table: "StripeInvoices",
                column: "SubscriptionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeInvoices");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "StripeSubscriptions",
                nullable: true);
        }
    }
}
