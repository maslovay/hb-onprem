using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class ChangeDialogueClientSatisfaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "DialogueClientSatisfactions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "DialogueClientSatisfactions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "DialogueClientSatisfactions");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "DialogueClientSatisfactions");
        }
    }
}
