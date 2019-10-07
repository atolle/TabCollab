using Microsoft.EntityFrameworkCore.Migrations;

namespace TabRepository.Migrations
{
    public partial class AddStripeTaxRate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StripeTaxRates",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    SubscriptionId = table.Column<string>(nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StripeDescription = table.Column<string>(nullable: true),
                    StripeJurisdicion = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true),
                    Zip = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeTaxRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StripeTaxRates_StripeSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "StripeSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StripeTaxRates_SubscriptionId",
                table: "StripeTaxRates",
                column: "SubscriptionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeTaxRates");
        }
    }
}
