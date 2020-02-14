using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class big_change_add_devices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_WorkerTypes_WorkerTypeId",
                table: "AspNetUsers");

         

            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
                table: "Dialogues");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Sessions_AspNetUsers_ApplicationUserId",
            //    table: "Sessions");

            migrationBuilder.DropTable(
                name: "LoginHistorys");

            migrationBuilder.DropTable(
                name: "PasswordHistorys");

            migrationBuilder.DropTable(
                name: "WorkerTypes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_WorkerTypeId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WorkerTypeId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "SlideShowSessions",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "ApplicationUserId",
            //    table: "Sessions",
            //    nullable: true,
            //    oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Sessions",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            //migrationBuilder.AlterColumn<Guid>(
            //    name: "ApplicationUserId",
            //    table: "Dialogues",
            //    nullable: true,
            //    oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Dialogues",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "CampaignContentAnswers",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "CampaignContentAnswers",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Alerts",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DeviceTypes",
                columns: table => new
                {
                    DeviceTypeId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTypes", x => x.DeviceTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    CompanyId = table.Column<Guid>(nullable: false),
                    DeviceTypeId = table.Column<Guid>(nullable: true),
                    StatusId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_Devices_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Devices_DeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "DeviceTypes",
                        principalColumn: "DeviceTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Statuss_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuss",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlideShowSessions_DeviceId",
                table: "SlideShowSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_DeviceId",
                table: "Sessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_DeviceId",
                table: "Dialogues",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentAnswers_DeviceId",
                table: "CampaignContentAnswers",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeviceId",
                table: "Alerts",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CompanyId",
                table: "Devices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceTypeId",
                table: "Devices",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_StatusId",
                table: "Devices",
                column: "StatusId");

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
            //    name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
            //    table: "Dialogues",
            //    column: "ApplicationUserId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Restrict);

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
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Devices_DeviceId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_CampaignContentAnswers_AspNetUsers_ApplicationUserId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_CampaignContentAnswers_Devices_DeviceId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
                table: "Dialogues");

            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_Devices_DeviceId",
                table: "Dialogues");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_AspNetUsers_ApplicationUserId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Devices_DeviceId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SlideShowSessions_Devices_DeviceId",
                table: "SlideShowSessions");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "DeviceTypes");

            migrationBuilder.DropIndex(
                name: "IX_SlideShowSessions_DeviceId",
                table: "SlideShowSessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_DeviceId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Dialogues_DeviceId",
                table: "Dialogues");

            migrationBuilder.DropIndex(
                name: "IX_CampaignContentAnswers_DeviceId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_DeviceId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "SlideShowSessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Dialogues");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Alerts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "Sessions",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "Dialogues",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "CampaignContentAnswers",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkerTypeId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoginHistorys",
                columns: table => new
                {
                    LoginHistoryId = table.Column<Guid>(nullable: false),
                    Attempt = table.Column<int>(nullable: false),
                    LoginTime = table.Column<DateTime>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistorys", x => x.LoginHistoryId);
                    table.ForeignKey(
                        name: "FK_LoginHistorys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistorys",
                columns: table => new
                {
                    PasswordHistoryId = table.Column<Guid>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistorys", x => x.PasswordHistoryId);
                    table.ForeignKey(
                        name: "FK_PasswordHistorys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkerTypes",
                columns: table => new
                {
                    WorkerTypeId = table.Column<Guid>(nullable: false),
                    CompanyId = table.Column<Guid>(nullable: false),
                    WorkerTypeName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerTypes", x => x.WorkerTypeId);
                    table.ForeignKey(
                        name: "FK_WorkerTypes_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_WorkerTypeId",
                table: "AspNetUsers",
                column: "WorkerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistorys_UserId",
                table: "LoginHistorys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistorys_UserId",
                table: "PasswordHistorys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerTypes_CompanyId",
                table: "WorkerTypes",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_WorkerTypes_WorkerTypeId",
                table: "AspNetUsers",
                column: "WorkerTypeId",
                principalTable: "WorkerTypes",
                principalColumn: "WorkerTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignContentAnswers_AspNetUsers_ApplicationUserId",
                table: "CampaignContentAnswers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
                table: "Dialogues",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_AspNetUsers_ApplicationUserId",
                table: "Sessions",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
