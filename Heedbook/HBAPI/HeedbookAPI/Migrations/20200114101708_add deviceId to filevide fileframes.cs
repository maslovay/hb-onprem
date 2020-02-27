using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class adddeviceIdtofilevidefileframes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_FileFrames_AspNetUsers_ApplicationUserId",
            //    table: "FileFrames");

            migrationBuilder.DropForeignKey(
                name: "FK_FileVideos_AspNetUsers_ApplicationUserId",
                table: "FileVideos");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "FileVideos",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "FileVideos",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "FileFrames",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "FileFrames",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FileVideos_DeviceId",
                table: "FileVideos",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_FileFrames_DeviceId",
                table: "FileFrames",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileFrames_AspNetUsers_ApplicationUserId",
                table: "FileFrames",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_FileFrames_Devices_DeviceId",
            //    table: "FileFrames",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileVideos_AspNetUsers_ApplicationUserId",
                table: "FileVideos",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_FileVideos_Devices_DeviceId",
            //    table: "FileVideos",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileFrames_AspNetUsers_ApplicationUserId",
                table: "FileFrames");

            migrationBuilder.DropForeignKey(
                name: "FK_FileFrames_Devices_DeviceId",
                table: "FileFrames");

            migrationBuilder.DropForeignKey(
                name: "FK_FileVideos_AspNetUsers_ApplicationUserId",
                table: "FileVideos");

            migrationBuilder.DropForeignKey(
                name: "FK_FileVideos_Devices_DeviceId",
                table: "FileVideos");

            migrationBuilder.DropIndex(
                name: "IX_FileVideos_DeviceId",
                table: "FileVideos");

            migrationBuilder.DropIndex(
                name: "IX_FileFrames_DeviceId",
                table: "FileFrames");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "FileVideos");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "FileFrames");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "FileVideos",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "FileFrames",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FileFrames_AspNetUsers_ApplicationUserId",
                table: "FileFrames",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileVideos_AspNetUsers_ApplicationUserId",
                table: "FileVideos",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
