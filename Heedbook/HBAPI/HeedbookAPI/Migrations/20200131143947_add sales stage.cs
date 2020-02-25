using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class addsalesstage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsClient",
                table: "Phrases");

            migrationBuilder.CreateTable(
                name: "SalesStages",
                columns: table => new
                {
                    SalesStageId = table.Column<Guid>(nullable: false),
                    SequenceNumber = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    CompanyId = table.Column<Guid>(nullable: true),
                    CorporationId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesStages", x => x.SalesStageId);
                    table.ForeignKey(
                        name: "FK_SalesStages_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesStages_Corporations_CorporationId",
                        column: x => x.CorporationId,
                        principalTable: "Corporations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesStages_CompanyId",
                table: "SalesStages",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStages_CorporationId",
                table: "SalesStages",
                column: "CorporationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesStages");

            migrationBuilder.AddColumn<bool>(
                name: "IsClient",
                table: "Phrases",
                nullable: false,
                defaultValue: false);
        }
    }
}
