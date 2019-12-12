using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class gendertostringinclients : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenderId",
                table: "Clients");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Clients",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Clients");

            migrationBuilder.AddColumn<int>(
                name: "GenderId",
                table: "Clients",
                nullable: false,
                defaultValue: 0);
        }
    }
}
