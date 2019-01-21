using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HBData.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    NormalizedName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "CompanyIndustries",
                columns: table => new
                {
                    CompanyIndustryId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CompanyIndustryName = table.Column<string>(nullable: true),
                    SatisfactionIndex = table.Column<double>(nullable: true),
                    LoadIndex = table.Column<double>(nullable: true),
                    CrossSalesIndex = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyIndustries", x => x.CompanyIndustryId);
                });

            migrationBuilder.CreateTable(
                name: "Corporations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corporations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    CountryId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CountryName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryId);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    LanguageName = table.Column<string>(nullable: true),
                    LanguageLocalName = table.Column<string>(nullable: true),
                    LanguageShortName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageId);
                });

            migrationBuilder.CreateTable(
                name: "PhraseTypes",
                columns: table => new
                {
                    PhraseTypeId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    PhraseTypeText = table.Column<string>(nullable: true),
                    Colour = table.Column<string>(nullable: true),
                    ColourSyn = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhraseTypes", x => x.PhraseTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    StatusId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    StatusName = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "Phrases",
                columns: table => new
                {
                    PhraseId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    PhraseText = table.Column<string>(nullable: true),
                    PhraseTypeId = table.Column<int>(nullable: true),
                    LanguageId = table.Column<int>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false),
                    WordsSpace = table.Column<int>(nullable: true),
                    Accurancy = table.Column<double>(nullable: true),
                    Template = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phrases", x => x.PhraseId);
                    table.ForeignKey(
                        name: "FK_Phrases_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Phrases_PhraseTypes_PhraseTypeId",
                        column: x => x.PhraseTypeId,
                        principalTable: "PhraseTypes",
                        principalColumn: "PhraseTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    CompanyId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CompanyName = table.Column<string>(nullable: false),
                    CompanyIndustryId = table.Column<int>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    LanguageId = table.Column<int>(nullable: false),
                    CountryId = table.Column<int>(nullable: true),
                    StatusId = table.Column<int>(nullable: true),
                    CorporationId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_Companies_CompanyIndustries_CompanyIndustryId",
                        column: x => x.CompanyIndustryId,
                        principalTable: "CompanyIndustries",
                        principalColumn: "CompanyIndustryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Companies_Corporations_CorporationId",
                        column: x => x.CorporationId,
                        principalTable: "Corporations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Companies_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Companies_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Companies_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    IsSplash = table.Column<bool>(nullable: false),
                    GenderId = table.Column<int>(nullable: false),
                    BegAge = table.Column<int>(nullable: true),
                    EndAge = table.Column<int>(nullable: true),
                    BegDate = table.Column<DateTime>(nullable: true),
                    EndDate = table.Column<DateTime>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: true),
                    CompanyId = table.Column<int>(nullable: false),
                    StatusId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.CampaignId);
                    table.ForeignKey(
                        name: "FK_Campaigns_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Campaigns_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contents",
                columns: table => new
                {
                    ContentId = table.Column<Guid>(nullable: false),
                    RawHTML = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Duration = table.Column<int>(nullable: false),
                    CompanyId = table.Column<int>(nullable: false),
                    JSONData = table.Column<string>(nullable: true),
                    IsTemplate = table.Column<bool>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: true),
                    UpdateDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contents", x => x.ContentId);
                    table.ForeignKey(
                        name: "FK_Contents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CompanyId = table.Column<int>(nullable: true),
                    StatusId = table.Column<int>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    TransactionId = table.Column<string>(nullable: true),
                    PaymentAmount = table.Column<double>(nullable: false),
                    PaymentTime = table.Column<double>(nullable: false),
                    PaymentComment = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhraseCompanies",
                columns: table => new
                {
                    PhraseCompanyId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    PhraseId = table.Column<int>(nullable: true),
                    CompanyId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhraseCompanies", x => x.PhraseCompanyId);
                    table.ForeignKey(
                        name: "FK_PhraseCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhraseCompanies_Phrases_PhraseId",
                        column: x => x.PhraseId,
                        principalTable: "Phrases",
                        principalColumn: "PhraseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tariffs",
                columns: table => new
                {
                    TariffId = table.Column<Guid>(nullable: false),
                    CompanyId = table.Column<int>(nullable: true),
                    CustomerKey = table.Column<string>(nullable: true),
                    TotalRate = table.Column<decimal>(nullable: false),
                    EmployeeNo = table.Column<int>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    ExpirationDate = table.Column<DateTime>(nullable: false),
                    Rebillid = table.Column<string>(nullable: true),
                    Token = table.Column<byte[]>(nullable: true),
                    StatusId = table.Column<int>(nullable: true),
                    isMonthly = table.Column<bool>(nullable: false),
                    TariffComment = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tariffs", x => x.TariffId);
                    table.ForeignKey(
                        name: "FK_Tariffs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tariffs_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerTypes",
                columns: table => new
                {
                    WorkerTypeId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CompanyId = table.Column<int>(nullable: false),
                    WorkerTypeName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerTypes", x => x.WorkerTypeId);
                    table.ForeignKey(
                        name: "FK_WorkerTypes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignContents",
                columns: table => new
                {
                    CampaignContentId = table.Column<Guid>(nullable: false),
                    SequenceNumber = table.Column<int>(nullable: false),
                    ContentId = table.Column<Guid>(nullable: true),
                    CampaignId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignContents", x => x.CampaignContentId);
                    table.ForeignKey(
                        name: "FK_CampaignContents_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "CampaignId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignContents_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "ContentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(nullable: false),
                    Amount = table.Column<decimal>(nullable: false),
                    OrderId = table.Column<string>(nullable: true),
                    PaymentId = table.Column<string>(nullable: true),
                    TariffId = table.Column<Guid>(nullable: true),
                    StatusId = table.Column<int>(nullable: true),
                    PaymentDate = table.Column<DateTime>(nullable: false),
                    TransactionComment = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_Transactions_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Tariffs_TariffId",
                        column: x => x.TariffId,
                        principalTable: "Tariffs",
                        principalColumn: "TariffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    FullName = table.Column<string>(nullable: true),
                    Avatar = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    EmployeeId = table.Column<string>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    CompanyId = table.Column<int>(nullable: true),
                    StatusId = table.Column<int>(nullable: true),
                    OneSignalId = table.Column<string>(nullable: true),
                    WorkerTypeId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUsers_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUsers_WorkerTypes_WorkerTypeId",
                        column: x => x.WorkerTypeId,
                        principalTable: "WorkerTypes",
                        principalColumn: "WorkerTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignContentSessions",
                columns: table => new
                {
                    CampaignContentSessionId = table.Column<Guid>(nullable: false),
                    BegTime = table.Column<DateTime>(nullable: false),
                    CampaignContentId = table.Column<Guid>(nullable: false),
                    ApplicationUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignContentSessions", x => x.CampaignContentSessionId);
                    table.ForeignKey(
                        name: "FK_CampaignContentSessions_ApplicationUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignContentSessions_CampaignContents_CampaignContentId",
                        column: x => x.CampaignContentId,
                        principalTable: "CampaignContents",
                        principalColumn: "CampaignContentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dialogues",
                columns: table => new
                {
                    DialogueId = table.Column<Guid>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    BegTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    LanguageId = table.Column<int>(nullable: true),
                    StatusId = table.Column<int>(nullable: true),
                    SysVersion = table.Column<string>(nullable: true),
                    InStatistic = table.Column<bool>(nullable: false),
                    Comment = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dialogues", x => x.DialogueId);
                    table.ForeignKey(
                        name: "FK_Dialogues_ApplicationUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dialogues_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dialogues_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    BegTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false),
                    StatusId = table.Column<int>(nullable: true),
                    IsDesktop = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_ApplicationUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueAudios",
                columns: table => new
                {
                    DialogueAudioId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false),
                    NeutralityTone = table.Column<double>(nullable: true),
                    PositiveTone = table.Column<double>(nullable: true),
                    NegativeTone = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueAudios", x => x.DialogueAudioId);
                    table.ForeignKey(
                        name: "FK_DialogueAudios_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueClientProfiles",
                columns: table => new
                {
                    DialogueClientProfileId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false),
                    Avatar = table.Column<string>(nullable: true),
                    Age = table.Column<double>(nullable: true),
                    Gender = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueClientProfiles", x => x.DialogueClientProfileId);
                    table.ForeignKey(
                        name: "FK_DialogueClientProfiles_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueClientSatisfactions",
                columns: table => new
                {
                    DialogueClientSatisfactionId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    MeetingExpectationsTotal = table.Column<double>(nullable: true),
                    BegMoodTotal = table.Column<double>(nullable: true),
                    EndMoodTotal = table.Column<double>(nullable: true),
                    MeetingExpectationsByClient = table.Column<double>(nullable: true),
                    MeetingExpectationsByEmpoyee = table.Column<double>(nullable: true),
                    BegMoodByEmpoyee = table.Column<double>(nullable: true),
                    EndMoodByEmpoyee = table.Column<double>(nullable: true),
                    MeetingExpectationsByTeacher = table.Column<double>(nullable: true),
                    BegMoodByTeacher = table.Column<double>(nullable: true),
                    EndMoodByTeacher = table.Column<double>(nullable: true),
                    MeetingExpectationsByNN = table.Column<double>(nullable: true),
                    BegMoodByNN = table.Column<double>(nullable: true),
                    EndMoodByNN = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueClientSatisfactions", x => x.DialogueClientSatisfactionId);
                    table.ForeignKey(
                        name: "FK_DialogueClientSatisfactions_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueFrames",
                columns: table => new
                {
                    DialogueFrameId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false),
                    Time = table.Column<DateTime>(nullable: false),
                    HappinessShare = table.Column<double>(nullable: true),
                    NeutralShare = table.Column<double>(nullable: true),
                    SurpriseShare = table.Column<double>(nullable: true),
                    SadnessShare = table.Column<double>(nullable: true),
                    AngerShare = table.Column<double>(nullable: true),
                    DisgustShare = table.Column<double>(nullable: true),
                    ContemptShare = table.Column<double>(nullable: true),
                    FearShare = table.Column<double>(nullable: true),
                    YawShare = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueFrames", x => x.DialogueFrameId);
                    table.ForeignKey(
                        name: "FK_DialogueFrames_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueHints",
                columns: table => new
                {
                    DialogueHintId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DialogueId = table.Column<Guid>(nullable: true),
                    HintText = table.Column<string>(nullable: true),
                    IsAutomatic = table.Column<bool>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    IsPositive = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueHints", x => x.DialogueHintId);
                    table.ForeignKey(
                        name: "FK_DialogueHints_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueIntervals",
                columns: table => new
                {
                    DialogueIntervalId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false),
                    BegTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false),
                    NeutralityTone = table.Column<double>(nullable: true),
                    HappinessTone = table.Column<double>(nullable: true),
                    SadnessTone = table.Column<double>(nullable: true),
                    AngerTone = table.Column<double>(nullable: true),
                    FearTone = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueIntervals", x => x.DialogueIntervalId);
                    table.ForeignKey(
                        name: "FK_DialogueIntervals_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialoguePhraseCounts",
                columns: table => new
                {
                    DialoguePhraseCountId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DialogueId = table.Column<Guid>(nullable: true),
                    PhraseTypeId = table.Column<int>(nullable: true),
                    PhraseCount = table.Column<int>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialoguePhraseCounts", x => x.DialoguePhraseCountId);
                    table.ForeignKey(
                        name: "FK_DialoguePhraseCounts_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DialoguePhraseCounts_PhraseTypes_PhraseTypeId",
                        column: x => x.PhraseTypeId,
                        principalTable: "PhraseTypes",
                        principalColumn: "PhraseTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialoguePhrasePlaces",
                columns: table => new
                {
                    DialoguePhrasePlaceId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DialogueId = table.Column<Guid>(nullable: true),
                    PhraseId = table.Column<int>(nullable: true),
                    WordPosition = table.Column<int>(nullable: true),
                    Synonyn = table.Column<bool>(nullable: false),
                    SynonynText = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialoguePhrasePlaces", x => x.DialoguePhrasePlaceId);
                    table.ForeignKey(
                        name: "FK_DialoguePhrasePlaces_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DialoguePhrasePlaces_Phrases_PhraseId",
                        column: x => x.PhraseId,
                        principalTable: "Phrases",
                        principalColumn: "PhraseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialoguePhrases",
                columns: table => new
                {
                    DialoguePhraseId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DialogueId = table.Column<Guid>(nullable: true),
                    PhraseId = table.Column<int>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialoguePhrases", x => x.DialoguePhraseId);
                    table.ForeignKey(
                        name: "FK_DialoguePhrases_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DialoguePhrases_Phrases_PhraseId",
                        column: x => x.PhraseId,
                        principalTable: "Phrases",
                        principalColumn: "PhraseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueSpeeches",
                columns: table => new
                {
                    DialogueSpeechId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false),
                    PositiveShare = table.Column<double>(nullable: true),
                    SpeechSpeed = table.Column<double>(nullable: true),
                    SilenceShare = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueSpeeches", x => x.DialogueSpeechId);
                    table.ForeignKey(
                        name: "FK_DialogueSpeeches_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueVisuals",
                columns: table => new
                {
                    DialogueVisualId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    AttentionShare = table.Column<double>(nullable: true),
                    HappinessShare = table.Column<double>(nullable: true),
                    NeutralShare = table.Column<double>(nullable: true),
                    SurpriseShare = table.Column<double>(nullable: true),
                    SadnessShare = table.Column<double>(nullable: true),
                    AngerShare = table.Column<double>(nullable: true),
                    DisgustShare = table.Column<double>(nullable: true),
                    ContemptShare = table.Column<double>(nullable: true),
                    FearShare = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueVisuals", x => x.DialogueVisualId);
                    table.ForeignKey(
                        name: "FK_DialogueVisuals_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DialogueWords",
                columns: table => new
                {
                    DialogueWordId = table.Column<Guid>(nullable: false),
                    DialogueId = table.Column<Guid>(nullable: true),
                    IsClient = table.Column<bool>(nullable: false),
                    Word = table.Column<string>(nullable: true),
                    BegTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogueWords", x => x.DialogueWordId);
                    table.ForeignKey(
                        name: "FK_DialogueWords_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "DialogueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_CompanyId",
                table: "ApplicationUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_StatusId",
                table: "ApplicationUsers",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_WorkerTypeId",
                table: "ApplicationUsers",
                column: "WorkerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContents_CampaignId",
                table: "CampaignContents",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContents_ContentId",
                table: "CampaignContents",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentSessions_ApplicationUserId",
                table: "CampaignContentSessions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentSessions_CampaignContentId",
                table: "CampaignContentSessions",
                column: "CampaignContentId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CompanyId",
                table: "Campaigns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StatusId",
                table: "Campaigns",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyIndustryId",
                table: "Companies",
                column: "CompanyIndustryId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CorporationId",
                table: "Companies",
                column: "CorporationId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CountryId",
                table: "Companies",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_LanguageId",
                table: "Companies",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_StatusId",
                table: "Companies",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_CompanyId",
                table: "Contents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueAudios_DialogueId",
                table: "DialogueAudios",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueClientProfiles_DialogueId",
                table: "DialogueClientProfiles",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueClientSatisfactions_DialogueId",
                table: "DialogueClientSatisfactions",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueFrames_DialogueId",
                table: "DialogueFrames",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueHints_DialogueId",
                table: "DialogueHints",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueIntervals_DialogueId",
                table: "DialogueIntervals",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialoguePhraseCounts_DialogueId",
                table: "DialoguePhraseCounts",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialoguePhraseCounts_PhraseTypeId",
                table: "DialoguePhraseCounts",
                column: "PhraseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DialoguePhrasePlaces_DialogueId",
                table: "DialoguePhrasePlaces",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialoguePhrasePlaces_PhraseId",
                table: "DialoguePhrasePlaces",
                column: "PhraseId");

            migrationBuilder.CreateIndex(
                name: "IX_DialoguePhrases_DialogueId",
                table: "DialoguePhrases",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialoguePhrases_PhraseId",
                table: "DialoguePhrases",
                column: "PhraseId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_ApplicationUserId",
                table: "Dialogues",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_LanguageId",
                table: "Dialogues",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_StatusId",
                table: "Dialogues",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueSpeeches_DialogueId",
                table: "DialogueSpeeches",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueVisuals_DialogueId",
                table: "DialogueVisuals",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogueWords_DialogueId",
                table: "DialogueWords",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId",
                table: "Payments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StatusId",
                table: "Payments",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PhraseCompanies_CompanyId",
                table: "PhraseCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PhraseCompanies_PhraseId",
                table: "PhraseCompanies",
                column: "PhraseId");

            migrationBuilder.CreateIndex(
                name: "IX_Phrases_LanguageId",
                table: "Phrases",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Phrases_PhraseTypeId",
                table: "Phrases",
                column: "PhraseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ApplicationUserId",
                table: "Sessions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StatusId",
                table: "Sessions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Tariffs_CompanyId",
                table: "Tariffs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Tariffs_StatusId",
                table: "Tariffs",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_StatusId",
                table: "Transactions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TariffId",
                table: "Transactions",
                column: "TariffId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerTypes_CompanyId",
                table: "WorkerTypes",
                column: "CompanyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "CampaignContentSessions");

            migrationBuilder.DropTable(
                name: "DialogueAudios");

            migrationBuilder.DropTable(
                name: "DialogueClientProfiles");

            migrationBuilder.DropTable(
                name: "DialogueClientSatisfactions");

            migrationBuilder.DropTable(
                name: "DialogueFrames");

            migrationBuilder.DropTable(
                name: "DialogueHints");

            migrationBuilder.DropTable(
                name: "DialogueIntervals");

            migrationBuilder.DropTable(
                name: "DialoguePhraseCounts");

            migrationBuilder.DropTable(
                name: "DialoguePhrasePlaces");

            migrationBuilder.DropTable(
                name: "DialoguePhrases");

            migrationBuilder.DropTable(
                name: "DialogueSpeeches");

            migrationBuilder.DropTable(
                name: "DialogueVisuals");

            migrationBuilder.DropTable(
                name: "DialogueWords");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PhraseCompanies");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "CampaignContents");

            migrationBuilder.DropTable(
                name: "Dialogues");

            migrationBuilder.DropTable(
                name: "Phrases");

            migrationBuilder.DropTable(
                name: "Tariffs");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "Contents");

            migrationBuilder.DropTable(
                name: "ApplicationUsers");

            migrationBuilder.DropTable(
                name: "PhraseTypes");

            migrationBuilder.DropTable(
                name: "WorkerTypes");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "CompanyIndustries");

            migrationBuilder.DropTable(
                name: "Corporations");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Statuses");
        }
    }
}
