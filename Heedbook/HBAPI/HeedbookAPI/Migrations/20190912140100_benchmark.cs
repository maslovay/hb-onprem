using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class benchmark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkNames",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkNames", x => x.Id);
                });

         

            migrationBuilder.CreateTable(
                name: "Benchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Day = table.Column<DateTime>(nullable: false),
                    IndustryId = table.Column<Guid>(nullable: true),
                    BenchmarkNameId = table.Column<Guid>(nullable: false),
                    Value = table.Column<double>(nullable: false),
                    Weight = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benchmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Benchmarks_BenchmarkNames_BenchmarkNameId",
                        column: x => x.BenchmarkNameId,
                        principalTable: "BenchmarkNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Benchmarks_CompanyIndustrys_IndustryId",
                        column: x => x.IndustryId,
                        principalTable: "CompanyIndustrys",
                        principalColumn: "CompanyIndustryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Benchmarks_BenchmarkNameId",
                table: "Benchmarks",
                column: "BenchmarkNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Benchmarks_IndustryId",
                table: "Benchmarks",
                column: "IndustryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Benchmarks");
        }
    }
}
