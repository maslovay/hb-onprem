using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class addclientsession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "ClientSessions",
            //    columns: table => new
            //    {
            //        ClientSessionId = table.Column<Guid>(nullable: false),
            //        Time = table.Column<DateTime>(nullable: false),
            //        ClientId = table.Column<Guid>(nullable: false),
            //        FileName = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ClientSessions", x => x.ClientSessionId);
            //        table.ForeignKey(
            //            name: "FK_ClientSessions_Clients_ClientId",
            //            column: x => x.ClientId,
            //            principalTable: "Clients",
            //            principalColumn: "ClientId",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_ClientSessions_ClientId",
            //    table: "ClientSessions",
            //    column: "ClientId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "ClientSessions");
        }
    }
}
