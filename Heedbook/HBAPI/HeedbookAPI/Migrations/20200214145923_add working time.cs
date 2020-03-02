using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class addworkingtime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkingTimes",
                columns: table => new
                {
                    WorkingTimeId = table.Column<Guid>(nullable: false),
                    Day = table.Column<int>(nullable: false),
                    CompanyId = table.Column<Guid>(nullable: false),
                    BegTime = table.Column<DateTime>(nullable: true),
                    EndTime = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingTimes", x => x.WorkingTimeId);
                    table.ForeignKey(
                        name: "FK_WorkingTimes_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkingTimes_CompanyId",
                table: "WorkingTimes",
                column: "CompanyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkingTimes");
        }
    }
}
