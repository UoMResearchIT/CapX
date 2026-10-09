// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PPMTool.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class ReworkAllFinancialReferenceRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CostValueSetId",
                table: "WorkloadModelChanges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinancialReferenceValueSets",
                columns: table => new
                {
                    FinancialReferenceValueSetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReferenceValueSets", x => x.FinancialReferenceValueSetId);
                });

            migrationBuilder.CreateTable(
                name: "FinancialReferenceValues",
                columns: table => new
                {
                    FinancialReferenceValueId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancialReferenceValueSetId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false),
                    FinancialReferenceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReferenceValues", x => x.FinancialReferenceValueId);
                    table.ForeignKey(
                        name: "FK_FinancialReferenceValues_FinancialReferenceValueSets_Financ~",
                        column: x => x.FinancialReferenceValueSetId,
                        principalTable: "FinancialReferenceValueSets",
                        principalColumn: "FinancialReferenceValueSetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialReferenceValues_FinancialReferences_FinancialRefer~",
                        column: x => x.FinancialReferenceId,
                        principalTable: "FinancialReferences",
                        principalColumn: "FinancialReferenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadModelChanges_CostValueSetId",
                table: "WorkloadModelChanges",
                column: "CostValueSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReferenceValues_FinancialReferenceId",
                table: "FinancialReferenceValues",
                column: "FinancialReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReferenceValues_FinancialReferenceValueSetId",
                table: "FinancialReferenceValues",
                column: "FinancialReferenceValueSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkloadModelChanges_FinancialReferenceValueSets_CostValueS~",
                table: "WorkloadModelChanges",
                column: "CostValueSetId",
                principalTable: "FinancialReferenceValueSets",
                principalColumn: "FinancialReferenceValueSetId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(@"
                INSERT INTO ""FinancialReferenceValueSets"" (""Name"")
                VALUES ('Grade41Costs'), ('Grade51Costs'), ('Grade55Costs'), ('Grade65Costs'), ('Grade71Costs'), ('Grade75Costs'), ('RecoveryTarget');

                INSERT INTO ""FinancialReferenceValues"" (""FinancialReferenceValueSetId"", ""Value"", ""FinancialReferenceId"")
                SELECT fvs.""FinancialReferenceValueSetId"", fr.""Grade41Costs"", fr.""FinancialReferenceId""
                FROM ""FinancialReferences"" fr
                CROSS JOIN ""FinancialReferenceValueSets"" fvs
                WHERE fvs.""Name"" = 'Grade41Costs'
                UNION ALL
                SELECT fvs.""FinancialReferenceValueSetId"", fr.""Grade51Costs"", fr.""FinancialReferenceId""
                FROM ""FinancialReferences"" fr
                CROSS JOIN ""FinancialReferenceValueSets"" fvs
                WHERE fvs.""Name"" = 'Grade51Costs'
                UNION ALL
                SELECT fvs.""FinancialReferenceValueSetId"", fr.""Grade55Costs"", fr.""FinancialReferenceId""
                FROM ""FinancialReferences"" fr
                CROSS JOIN ""FinancialReferenceValueSets"" fvs
                WHERE fvs.""Name"" = 'Grade55Costs'
                UNION ALL
                SELECT fvs.""FinancialReferenceValueSetId"", fr.""Grade65Costs"", fr.""FinancialReferenceId""
                FROM ""FinancialReferences"" fr
                CROSS JOIN ""FinancialReferenceValueSets"" fvs
                WHERE fvs.""Name"" = 'Grade65Costs'
                UNION ALL
                SELECT fvs.""FinancialReferenceValueSetId"", fr.""Grade71Costs"", fr.""FinancialReferenceId""
                FROM ""FinancialReferences"" fr
                CROSS JOIN ""FinancialReferenceValueSets"" fvs
                WHERE fvs.""Name"" = 'Grade71Costs'
                UNION ALL
                SELECT fvs.""FinancialReferenceValueSetId"", fr.""Grade75Costs"", fr.""FinancialReferenceId""
                FROM ""FinancialReferences"" fr
                CROSS JOIN ""FinancialReferenceValueSets"" fvs
                WHERE fvs.""Name"" = 'Grade75Costs'
                UNION ALL
                SELECT fvs.""FinancialReferenceValueSetId"", fr.""RecoveryTarget"", fr.""FinancialReferenceId""
                FROM ""FinancialReferences"" fr
                CROSS JOIN ""FinancialReferenceValueSets"" fvs
                WHERE fvs.""Name"" = 'RecoveryTarget';

                UPDATE ""WorkloadModelChanges""
                SET ""CostValueSetId"" = (
                    SELECT ""FinancialReferenceValueSetId"" FROM ""FinancialReferenceValueSets"" WHERE ""Name"" =
                        CASE
                            WHEN ""Grade"" = 4 THEN 'Grade41Costs'
                            WHEN ""Grade"" = 5 THEN 'Grade51Costs'
                            WHEN ""Grade"" = 6 THEN 'Grade65Costs'
                            WHEN ""Grade"" = 7 THEN 'Grade75Costs'
                            ELSE NULL
                        END
                )
                WHERE ""Grade"" IN (4,5,6,7);
            ");

            migrationBuilder.DropColumn(
                name: "Grade41Costs",
                table: "FinancialReferences");

            migrationBuilder.DropColumn(
                name: "Grade51Costs",
                table: "FinancialReferences");

            migrationBuilder.DropColumn(
                name: "Grade55Costs",
                table: "FinancialReferences");

            migrationBuilder.DropColumn(
                name: "Grade65Costs",
                table: "FinancialReferences");

            migrationBuilder.DropColumn(
                name: "Grade71Costs",
                table: "FinancialReferences");

            migrationBuilder.DropColumn(
                name: "Grade75Costs",
                table: "FinancialReferences");

            migrationBuilder.DropColumn(
                name: "RecoveryTarget",
                table: "FinancialReferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkloadModelChanges_FinancialReferenceValueSets_CostValueS~",
                table: "WorkloadModelChanges");

            migrationBuilder.DropTable(
                name: "FinancialReferenceValues");

            migrationBuilder.DropTable(
                name: "FinancialReferenceValueSets");

            migrationBuilder.DropIndex(
                name: "IX_WorkloadModelChanges_CostValueSetId",
                table: "WorkloadModelChanges");

            migrationBuilder.DropColumn(
                name: "CostValueSetId",
                table: "WorkloadModelChanges");

            migrationBuilder.AddColumn<float>(
                name: "Grade41Costs",
                table: "FinancialReferences",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Grade51Costs",
                table: "FinancialReferences",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Grade55Costs",
                table: "FinancialReferences",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Grade65Costs",
                table: "FinancialReferences",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Grade71Costs",
                table: "FinancialReferences",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Grade75Costs",
                table: "FinancialReferences",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RecoveryTarget",
                table: "FinancialReferences",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
