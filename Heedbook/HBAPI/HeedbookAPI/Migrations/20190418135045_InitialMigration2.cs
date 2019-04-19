using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class InitialMigration2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignContentSessions");

            migrationBuilder.RenameColumn(
                name: "Template",
                table: "Phrases",
                newName: "IsTemplate");

            migrationBuilder.AddColumn<string>(
                name: "STTResult",
                table: "FileAudioDialogues",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SlideShowSessions",
                columns: table => new
                {
                    SlideShowSessionId = table.Column<Guid>(nullable: false),
                    BegTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false),
                    CampaignContentId = table.Column<Guid>(nullable: true),
                    ContentType = table.Column<string>(nullable: true),
                    ApplicationUserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlideShowSessions", x => x.SlideShowSessionId);
                    table.ForeignKey(
                        name: "FK_SlideShowSessions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlideShowSessions_CampaignContents_CampaignContentId",
                        column: x => x.CampaignContentId,
                        principalTable: "CampaignContents",
                        principalColumn: "CampaignContentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlideShowSessions_ApplicationUserId",
                table: "SlideShowSessions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SlideShowSessions_CampaignContentId",
                table: "SlideShowSessions",
                column: "CampaignContentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlideShowSessions");

            migrationBuilder.DropColumn(
                name: "STTResult",
                table: "FileAudioDialogues");

            migrationBuilder.RenameColumn(
                name: "IsTemplate",
                table: "Phrases",
                newName: "Template");

            migrationBuilder.CreateTable(
                name: "CampaignContentSessions",
                columns: table => new
                {
                    CampaignContentSessionId = table.Column<Guid>(nullable: false),
                    ApplicationUserId = table.Column<Guid>(nullable: true),
                    BegTime = table.Column<DateTime>(nullable: false),
                    CampaignContentId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignContentSessions", x => x.CampaignContentSessionId);
                    table.ForeignKey(
                        name: "FK_CampaignContentSessions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignContentSessions_CampaignContents_CampaignContentId",
                        column: x => x.CampaignContentId,
                        principalTable: "CampaignContents",
                        principalColumn: "CampaignContentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentSessions_ApplicationUserId",
                table: "CampaignContentSessions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentSessions_CampaignContentId",
                table: "CampaignContentSessions",
                column: "CampaignContentId");
        }
    }
}
