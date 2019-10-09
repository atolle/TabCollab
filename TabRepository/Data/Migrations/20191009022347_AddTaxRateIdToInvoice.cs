using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class AddTaxRateIdToInvoice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaxRateId",
                table: "StripeInvoices",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeInvoices_TaxRateId",
                table: "StripeInvoices",
                column: "TaxRateId");

            migrationBuilder.AddForeignKey(
                name: "FK_StripeInvoices_StripeTaxRates_TaxRateId",
                table: "StripeInvoices",
                column: "TaxRateId",
                principalTable: "StripeTaxRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeInvoices_StripeTaxRates_TaxRateId",
                table: "StripeInvoices");

            migrationBuilder.DropIndex(
                name: "IX_StripeInvoices_TaxRateId",
                table: "StripeInvoices");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                table: "StripeInvoices");
        }
    }
}
