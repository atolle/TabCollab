using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class ReworkForeignKeysStripe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeCustomers_StripeSubscriptions_SubscriptionId",
                table: "StripeCustomers");

            migrationBuilder.DropIndex(
                name: "IX_StripeCustomers_SubscriptionId",
                table: "StripeCustomers");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "StripeCustomers");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "StripeSubscriptions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeSubscriptions_CustomerId",
                table: "StripeSubscriptions",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_StripeSubscriptions_StripeCustomers_CustomerId",
                table: "StripeSubscriptions",
                column: "CustomerId",
                principalTable: "StripeCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeSubscriptions_StripeCustomers_CustomerId",
                table: "StripeSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_StripeSubscriptions_CustomerId",
                table: "StripeSubscriptions");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "StripeSubscriptions");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionId",
                table: "StripeCustomers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeCustomers_SubscriptionId",
                table: "StripeCustomers",
                column: "SubscriptionId",
                unique: true,
                filter: "[SubscriptionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StripeCustomers_StripeSubscriptions_SubscriptionId",
                table: "StripeCustomers",
                column: "SubscriptionId",
                principalTable: "StripeSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
