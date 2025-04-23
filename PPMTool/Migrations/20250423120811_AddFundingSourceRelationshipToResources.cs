using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingSourceRelationshipToResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FundedFromFundingSourceId",
                table: "Resources",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_FundedFromFundingSourceId",
                table: "Resources",
                column: "FundedFromFundingSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_FundingSources_FundedFromFundingSourceId",
                table: "Resources",
                column: "FundedFromFundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "FundingSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_FundingSources_FundedFromFundingSourceId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_FundedFromFundingSourceId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "FundedFromFundingSourceId",
                table: "Resources");
        }
    }
}
