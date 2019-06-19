using System;
using HBData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class Createviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPoll",
                table: "SlideShowSessions",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "SlideShowSessions",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "CampaignContentAnswers",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "LoginHistorys",
                columns: table => new
                {
                    LoginHistoryId = table.Column<Guid>(nullable: false),
                    LoginTime = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    Attempt = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistorys", x => x.LoginHistoryId);
                    table.ForeignKey(
                        name: "FK_LoginHistorys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistorys",
                columns: table => new
                {
                    PasswordHistoryId = table.Column<Guid>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistorys", x => x.PasswordHistoryId);
                    table.ForeignKey(
                        name: "FK_PasswordHistorys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });       
        

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentAnswers_ApplicationUserId",
                table: "CampaignContentAnswers",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistorys_UserId",
                table: "LoginHistorys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistorys_UserId",
                table: "PasswordHistorys",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignContentAnswers_AspNetUsers_ApplicationUserId",
                table: "CampaignContentAnswers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CampaignContentAnswers_AspNetUsers_ApplicationUserId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropTable(
                name: "LoginHistorys");

            migrationBuilder.DropTable(
                name: "PasswordHistorys");

            migrationBuilder.DropIndex(
                name: "IX_CampaignContentAnswers_ApplicationUserId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropColumn(
                name: "IsPoll",
                table: "SlideShowSessions");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "SlideShowSessions");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "CampaignContentAnswers");
        }
    }
}
