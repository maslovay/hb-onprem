using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class referenceonclientindialogue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "Dialogues",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_ClientId",
                table: "Dialogues",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dialogues_Clients_ClientId",
                table: "Dialogues",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "ClientId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_Clients_ClientId",
                table: "Dialogues");

            migrationBuilder.DropIndex(
                name: "IX_Dialogues_ClientId",
                table: "Dialogues");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Dialogues");
        }
    }
}
