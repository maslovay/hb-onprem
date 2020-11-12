using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace UserOperations.Migrations
{
    public partial class merge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddForeignKey(
            //    name: "FK_FileFrames_Devices_DeviceId",
            //    table: "FileFrames",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_FileVideos_Devices_DeviceId",
            //    table: "FileVideos",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);

          

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Alerts_Devices_DeviceId",
            //    table: "Alerts",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_CampaignContentAnswers_AspNetUsers_ApplicationUserId",
            //    table: "CampaignContentAnswers",
            //    column: "ApplicationUserId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Restrict);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_CampaignContentAnswers_Devices_DeviceId",
            //    table: "CampaignContentAnswers",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);

          

            //migrationBuilder.AddForeignKey(
            //    name: "FK_SlideShowSessions_Devices_DeviceId",
            //    table: "SlideShowSessions",
            //    column: "DeviceId",
            //    principalTable: "Devices",
            //    principalColumn: "DeviceId",
            //    onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            //migrationBuilder.DropForeignKey(
            //    name: "FK_FileFrames_Devices_DeviceId",
            //    table: "FileFrames");


            //migrationBuilder.DropForeignKey(
            //    name: "FK_FileVideos_Devices_DeviceId",
            //    table: "FileVideos");

            //migrationBuilder.AlterColumn<bool>(
            //    name: "IsExtended",
            //    table: "Companys",
            //    nullable: true,
            //    oldClrType: typeof(bool));
        }
    }
}
