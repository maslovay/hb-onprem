using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class adddeviceIdtoslideshowsession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DialogueId",
                table: "SlideShowSessions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlideShowSessions_DialogueId",
                table: "SlideShowSessions",
                column: "DialogueId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlideShowSessions_Dialogues_DialogueId",
                table: "SlideShowSessions",
                column: "DialogueId",
                principalTable: "Dialogues",
                principalColumn: "DialogueId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlideShowSessions_Dialogues_DialogueId",
                table: "SlideShowSessions");

            migrationBuilder.DropIndex(
                name: "IX_SlideShowSessions_DialogueId",
                table: "SlideShowSessions");

            migrationBuilder.DropColumn(
                name: "DialogueId",
                table: "SlideShowSessions");
        }
    }
}
