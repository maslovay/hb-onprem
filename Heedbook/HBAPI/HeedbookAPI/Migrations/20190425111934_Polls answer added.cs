using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class Pollsansweradded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignContentAnswers",
                columns: table => new
                {
                    CampaignContentAnswerId = table.Column<Guid>(nullable: false),
                    Answer = table.Column<string>(nullable: true),
                    CampaignContentId = table.Column<Guid>(nullable: false),
                    Time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignContentAnswers", x => x.CampaignContentAnswerId);
                    table.ForeignKey(
                        name: "FK_CampaignContentAnswers_CampaignContents_CampaignContentId",
                        column: x => x.CampaignContentId,
                        principalTable: "CampaignContents",
                        principalColumn: "CampaignContentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentAnswers_CampaignContentId",
                table: "CampaignContentAnswers",
                column: "CampaignContentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignContentAnswers");
        }
    }
}
