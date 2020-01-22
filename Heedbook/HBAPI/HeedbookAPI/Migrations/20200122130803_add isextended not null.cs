using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class addisextendednotnull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsExtended",
                table: "Companys",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsExtended",
                table: "Companys",
                nullable: true,
                oldClrType: typeof(bool));
        }
    }
}
