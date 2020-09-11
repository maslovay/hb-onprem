using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class UpgradeMigrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_AspNetUsers_ApplicationUserId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_WorkerTypes_WorkerTypeId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_CampaignContentAnswers_AspNetUsers_ApplicationUserId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
                table: "Dialogues");

            migrationBuilder.DropForeignKey(
                name: "FK_FileFrames_AspNetUsers_ApplicationUserId",
                table: "FileFrames");

            migrationBuilder.DropForeignKey(
                name: "FK_FileVideos_AspNetUsers_ApplicationUserId",
                table: "FileVideos");

            migrationBuilder.DropTable(
                name: "LoginHistorys");

            migrationBuilder.DropTable(
                name: "PasswordHistorys");

            migrationBuilder.DropTable(
                name: "VIndexesByCompanysDays");

            migrationBuilder.DropTable(
                name: "WorkerTypes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_WorkerTypeId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsClient",
                table: "Phrases");

            migrationBuilder.DropColumn(
                name: "WorkerTypeId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                table: "Dialogues",
                newName: "ClientId");

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "SlideShowSessions",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DialogueId",
                table: "SlideShowSessions",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Sessions",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "Dialogues",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Dialogues",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<double>(
                name: "Age",
                table: "DialogueClientSatisfactions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "DialogueClientSatisfactions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "Contents",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExtended",
                table: "Companys",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "CampaignContents",
                nullable: true);

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

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "Alerts",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Alerts",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "BenchmarkNames",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Gender = table.Column<string>(nullable: true),
                    Age = table.Column<int>(nullable: false),
                    FaceDescriptor = table.Column<double[]>(nullable: true),
                    Avatar = table.Column<string>(nullable: true),
                    LastDate = table.Column<DateTime>(nullable: false),
                    StatusId = table.Column<int>(nullable: true),
                    CompanyId = table.Column<Guid>(nullable: false),
                    CorporationId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_Clients_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Clients_Corporations_CorporationId",
                        column: x => x.CorporationId,
                        principalTable: "Corporations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clients_Statuss_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuss",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "SalesStages",
                columns: table => new
                {
                    SalesStageId = table.Column<Guid>(nullable: false),
                    SequenceNumber = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesStages", x => x.SalesStageId);
                });

            migrationBuilder.CreateTable(
                name: "WorkingTimes",
                columns: table => new
                {
                    Day = table.Column<int>(nullable: false),
                    CompanyId = table.Column<Guid>(nullable: false),
                    BegTime = table.Column<DateTime>(nullable: true),
                    EndTime = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingTimes", x => new { x.Day, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_WorkingTimes_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Benchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Day = table.Column<DateTime>(nullable: false),
                    IndustryId = table.Column<Guid>(nullable: true),
                    BenchmarkNameId = table.Column<Guid>(nullable: false),
                    Value = table.Column<double>(nullable: false),
                    Weight = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benchmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Benchmarks_BenchmarkNames_BenchmarkNameId",
                        column: x => x.BenchmarkNameId,
                        principalTable: "BenchmarkNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Benchmarks_CompanyIndustrys_IndustryId",
                        column: x => x.IndustryId,
                        principalTable: "CompanyIndustrys",
                        principalColumn: "CompanyIndustryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientNotes",
                columns: table => new
                {
                    ClientNoteId = table.Column<Guid>(nullable: false),
                    ClientId = table.Column<Guid>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    ApplicationUserId = table.Column<Guid>(nullable: true),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientNotes", x => x.ClientNoteId);
                    table.ForeignKey(
                        name: "FK_ClientNotes_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientNotes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientSessions",
                columns: table => new
                {
                    ClientSessionId = table.Column<Guid>(nullable: false),
                    Time = table.Column<DateTime>(nullable: false),
                    ClientId = table.Column<Guid>(nullable: false),
                    FileName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSessions", x => x.ClientSessionId);
                    table.ForeignKey(
                        name: "FK_ClientSessions_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    CompanyId = table.Column<Guid>(nullable: false),
                    DeviceTypeId = table.Column<Guid>(nullable: true),
                    StatusId = table.Column<int>(nullable: false)
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesStagePhrases",
                columns: table => new
                {
                    SalesStagePhraseId = table.Column<Guid>(nullable: false),
                    CompanyId = table.Column<Guid>(nullable: true),
                    CorporationId = table.Column<Guid>(nullable: true),
                    PhraseId = table.Column<Guid>(nullable: false),
                    SalesStageId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesStagePhrases", x => x.SalesStagePhraseId);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_Companys_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companys",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_Corporations_CorporationId",
                        column: x => x.CorporationId,
                        principalTable: "Corporations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_Phrases_PhraseId",
                        column: x => x.PhraseId,
                        principalTable: "Phrases",
                        principalColumn: "PhraseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesStagePhrases_SalesStages_SalesStageId",
                        column: x => x.SalesStageId,
                        principalTable: "SalesStages",
                        principalColumn: "SalesStageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlideShowSessions_DeviceId",
                table: "SlideShowSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlideShowSessions_DialogueId",
                table: "SlideShowSessions",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_DeviceId",
                table: "Sessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVideos_DeviceId",
                table: "FileVideos",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_FileFrames_DeviceId",
                table: "FileFrames",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_ClientId",
                table: "Dialogues",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_DeviceId",
                table: "Dialogues",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_StatusId",
                table: "Contents",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContents_StatusId",
                table: "CampaignContents",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentAnswers_DeviceId",
                table: "CampaignContentAnswers",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeviceId",
                table: "Alerts",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Benchmarks_BenchmarkNameId",
                table: "Benchmarks",
                column: "BenchmarkNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Benchmarks_IndustryId",
                table: "Benchmarks",
                column: "IndustryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_ApplicationUserId",
                table: "ClientNotes",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_ClientId",
                table: "ClientNotes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CompanyId",
                table: "Clients",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CorporationId",
                table: "Clients",
                column: "CorporationId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_StatusId",
                table: "Clients",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSessions_ClientId",
                table: "ClientSessions",
                column: "ClientId");

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

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_CompanyId",
                table: "SalesStagePhrases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_CorporationId",
                table: "SalesStagePhrases",
                column: "CorporationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_PhraseId",
                table: "SalesStagePhrases",
                column: "PhraseId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesStagePhrases_SalesStageId",
                table: "SalesStagePhrases",
                column: "SalesStageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingTimes_CompanyId",
                table: "WorkingTimes",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_AspNetUsers_ApplicationUserId",
                table: "Alerts",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Devices_DeviceId",
                table: "Alerts",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignContentAnswers_AspNetUsers_ApplicationUserId",
                table: "CampaignContentAnswers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignContentAnswers_Devices_DeviceId",
                table: "CampaignContentAnswers",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignContents_Statuss_StatusId",
                table: "CampaignContents",
                column: "StatusId",
                principalTable: "Statuss",
                principalColumn: "StatusId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contents_Statuss_StatusId",
                table: "Contents",
                column: "StatusId",
                principalTable: "Statuss",
                principalColumn: "StatusId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
                table: "Dialogues",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dialogues_Clients_ClientId",
                table: "Dialogues",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "ClientId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dialogues_Devices_DeviceId",
                table: "Dialogues",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileFrames_AspNetUsers_ApplicationUserId",
                table: "FileFrames",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileFrames_Devices_DeviceId",
                table: "FileFrames",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileVideos_AspNetUsers_ApplicationUserId",
                table: "FileVideos",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileVideos_Devices_DeviceId",
                table: "FileVideos",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Devices_DeviceId",
                table: "Sessions",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlideShowSessions_Devices_DeviceId",
                table: "SlideShowSessions",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlideShowSessions_Dialogues_DialogueId",
                table: "SlideShowSessions",
                column: "DialogueId",
                principalTable: "Dialogues",
                principalColumn: "DialogueId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_AspNetUsers_ApplicationUserId",
                table: "Alerts");

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
                name: "FK_CampaignContents_Statuss_StatusId",
                table: "CampaignContents");

            migrationBuilder.DropForeignKey(
                name: "FK_Contents_Statuss_StatusId",
                table: "Contents");

            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
                table: "Dialogues");

            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_Clients_ClientId",
                table: "Dialogues");

            migrationBuilder.DropForeignKey(
                name: "FK_Dialogues_Devices_DeviceId",
                table: "Dialogues");

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

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Devices_DeviceId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SlideShowSessions_Devices_DeviceId",
                table: "SlideShowSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SlideShowSessions_Dialogues_DialogueId",
                table: "SlideShowSessions");

            migrationBuilder.DropTable(
                name: "Benchmarks");

            migrationBuilder.DropTable(
                name: "ClientNotes");

            migrationBuilder.DropTable(
                name: "ClientSessions");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "SalesStagePhrases");

            migrationBuilder.DropTable(
                name: "WorkingTimes");

            migrationBuilder.DropTable(
                name: "BenchmarkNames");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "DeviceTypes");

            migrationBuilder.DropTable(
                name: "SalesStages");

            migrationBuilder.DropIndex(
                name: "IX_SlideShowSessions_DeviceId",
                table: "SlideShowSessions");

            migrationBuilder.DropIndex(
                name: "IX_SlideShowSessions_DialogueId",
                table: "SlideShowSessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_DeviceId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_FileVideos_DeviceId",
                table: "FileVideos");

            migrationBuilder.DropIndex(
                name: "IX_FileFrames_DeviceId",
                table: "FileFrames");

            migrationBuilder.DropIndex(
                name: "IX_Dialogues_ClientId",
                table: "Dialogues");

            migrationBuilder.DropIndex(
                name: "IX_Dialogues_DeviceId",
                table: "Dialogues");

            migrationBuilder.DropIndex(
                name: "IX_Contents_StatusId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_CampaignContents_StatusId",
                table: "CampaignContents");

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
                name: "DialogueId",
                table: "SlideShowSessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "FileVideos");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "FileFrames");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Dialogues");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "DialogueClientSatisfactions");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "DialogueClientSatisfactions");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "IsExtended",
                table: "Companys");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "CampaignContents");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "CampaignContentAnswers");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Dialogues",
                newName: "PersonId");

            migrationBuilder.AddColumn<bool>(
                name: "IsClient",
                table: "Phrases",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "Alerts",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

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
                name: "VIndexesByCompanysDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CompanyId = table.Column<Guid>(nullable: false),
                    CompanyIndustryId = table.Column<Guid>(nullable: false),
                    Day = table.Column<DateTime>(nullable: false),
                    DialoguesHours = table.Column<double>(nullable: true),
                    SatisfactionIndex = table.Column<double>(nullable: true),
                    SessionHours = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VIndexesByCompanysDays", x => x.Id);
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
                name: "FK_Alerts_AspNetUsers_ApplicationUserId",
                table: "Alerts",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
