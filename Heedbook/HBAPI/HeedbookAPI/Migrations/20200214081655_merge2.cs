using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace UserOperations.Migrations
{
    public partial class merge2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
              name: "IsExtended",
              table: "Companys",
              nullable: false,
              oldClrType: typeof(bool),
              oldNullable: true);

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "ApplicationUserId",
            //    table: "Dialogues",
            //    nullable: true,
            //    oldClrType: typeof(Guid));

            //migrationBuilder.AddForeignKey(
            //  name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
            //  table: "Dialogues",
            //  column: "ApplicationUserId",
            //  principalTable: "AspNetUsers",
            //  principalColumn: "Id",
            //  onDelete: ReferentialAction.Restrict);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Dialogues_Devices_DeviceId",
            //    table: "Dialogues",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Sessions_AspNetUsers_ApplicationUserId",
            //    table: "Sessions",
            //    column: "ApplicationUserId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Restrict);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Sessions_Devices_DeviceId",
            //    table: "Sessions",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
