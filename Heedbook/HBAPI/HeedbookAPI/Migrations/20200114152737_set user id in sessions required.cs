using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class setuseridinsessionsrequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Sessions_AspNetUsers_ApplicationUserId",
            //    table: "Sessions");

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "ApplicationUserId",
            //    table: "Sessions",
            //    nullable: false,
            //    oldClrType: typeof(Guid),
            //    oldNullable: true);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Sessions_AspNetUsers_ApplicationUserId",
            //    table: "Sessions",
            //    column: "ApplicationUserId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Sessions_AspNetUsers_ApplicationUserId",
            //    table: "Sessions");

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "ApplicationUserId",
            //    table: "Sessions",
            //    nullable: true,
            //    oldClrType: typeof(Guid));

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Sessions_AspNetUsers_ApplicationUserId",
            //    table: "Sessions",
            //    column: "ApplicationUserId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Restrict);
        }
    }
}
