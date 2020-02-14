using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class createcomplexkeyforworkingtime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkingTimes",
                table: "WorkingTimes");

            migrationBuilder.DropColumn(
                name: "WorkingTimeId",
                table: "WorkingTimes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkingTimes",
                table: "WorkingTimes",
                columns: new[] { "Day", "CompanyId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkingTimes",
                table: "WorkingTimes");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkingTimeId",
                table: "WorkingTimes",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkingTimes",
                table: "WorkingTimes",
                columns: new[] { "WorkingTimeId", "Day", "CompanyId" });
        }
    }
}
