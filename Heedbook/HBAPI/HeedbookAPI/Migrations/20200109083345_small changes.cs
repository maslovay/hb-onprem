using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class smallchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Statuss_StatusId",
                table: "Devices");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "Devices",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Statuss_StatusId",
                table: "Devices",
                column: "StatusId",
                principalTable: "Statuss",
                principalColumn: "StatusId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Statuss_StatusId",
                table: "Devices");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "Devices",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Statuss_StatusId",
                table: "Devices",
                column: "StatusId",
                principalTable: "Statuss",
                principalColumn: "StatusId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
