using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class addsalesstagesphrasestable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesStages_Companys_CompanyId",
                table: "SalesStages");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesStages_Corporations_CorporationId",
                table: "SalesStages");

            migrationBuilder.DropIndex(
                name: "IX_SalesStages_CompanyId",
                table: "SalesStages");

            migrationBuilder.DropIndex(
                name: "IX_SalesStages_CorporationId",
                table: "SalesStages");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesStages");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "SalesStages");

            migrationBuilder.CreateTable(
                name: "SalesStagePhrases",
                columns: table => new
                {
                    SalesStagePhraseId = table.Column<Guid>(nullable: false),
                    CompanyId = table.Column<Guid>(nullable: true),
                    CorporationId = table.Column<Guid>(nullable: true),
                    PhraseId = table.Column<Guid>(nullable: false),
                    SalesStageId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesStagePhrases", x => x.SalesStagePhraseId);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_Corporations_CorporationId",
                        column: x => x.CorporationId,
                        principalTable: "Corporations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_Phrases_PhraseId",
                        column: x => x.PhraseId,
                        principalTable: "Phrases",
                        principalColumn: "PhraseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_SalesStages_SalesStageId",
                        column: x => x.SalesStageId,
                        principalTable: "SalesStages",
                        principalColumn: "SalesStageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_CompanyId",
                table: "SalesStagePhrases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_CorporationId",
                table: "SalesStagePhrases",
                column: "CorporationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_PhraseId",
                table: "SalesStagePhrases",
                column: "PhraseId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_SalesStageId",
                table: "SalesStagePhrases",
                column: "SalesStageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesStagePhrases");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "SalesStages",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorporationId",
                table: "SalesStages",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesStages_CompanyId",
                table: "SalesStages",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStages_CorporationId",
                table: "SalesStages",
                column: "CorporationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesStages_Companys_CompanyId",
                table: "SalesStages",
                column: "CompanyId",
                principalTable: "Companys",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesStages_Corporations_CorporationId",
                table: "SalesStages",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
