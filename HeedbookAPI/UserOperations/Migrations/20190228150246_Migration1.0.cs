using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class Migration10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Descriptor",
                table: "FrameAttributes",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "FrameAttributes",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descriptor",
                table: "FrameAttributes");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "FrameAttributes");
        }
    }
}
