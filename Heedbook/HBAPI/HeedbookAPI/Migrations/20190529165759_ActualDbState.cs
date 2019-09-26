//using System;
//using Microsoft.EntityFrameworkCore.Migrations;
//using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

//namespace UserOperations.Migrations
//{
//    public partial class ActualDbState : Migration
//    {
//        protected override void Up(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.CreateTable(
//                name: "AspNetRole",
//                columns: table => new
//                {
//                    Id = table.Column<Guid>(nullable: false),
//                    Name = table.Column<string>(maxLength: 256, nullable: true),
//                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
//                    ConcurrencyStamp = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_AspNetRole", x => x.Id);
//                });

//            migrationBuilder.CreateTable(
//                name: "CatalogueHints",
//                columns: table => new
//                {
//                    CatalogueHintId = table.Column<Guid>(nullable: false),
//                    HintCondition = table.Column<string>(nullable: true),
//                    HintText = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_CatalogueHints", x => x.CatalogueHintId);
//                });

//            migrationBuilder.CreateTable(
//                name: "CompanyIndustrys",
//                columns: table => new
//                {
//                    CompanyIndustryId = table.Column<Guid>(nullable: false),
//                    CompanyIndustryName = table.Column<string>(nullable: true),
//                    SatisfactionIndex = table.Column<double>(nullable: true),
//                    LoadIndex = table.Column<double>(nullable: true),
//                    CrossSalesIndex = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_CompanyIndustrys", x => x.CompanyIndustryId);
//                });

//            migrationBuilder.CreateTable(
//                name: "Corporations",
//                columns: table => new
//                {
//                    Id = table.Column<Guid>(nullable: false),
//                    Name = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Corporations", x => x.Id);
//                });

//            migrationBuilder.CreateTable(
//                name: "Countrys",
//                columns: table => new
//                {
//                    CountryId = table.Column<Guid>(nullable: false),
//                    CountryName = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Countrys", x => x.CountryId);
//                });

//            migrationBuilder.CreateTable(
//                name: "Languages",
//                columns: table => new
//                {
//                    LanguageId = table.Column<int>(nullable: false)
//                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
//                    LanguageName = table.Column<string>(nullable: true),
//                    LanguageLocalName = table.Column<string>(nullable: true),
//                    LanguageShortName = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Languages", x => x.LanguageId);
//                });

//            migrationBuilder.CreateTable(
//                name: "PhraseTypes",
//                columns: table => new
//                {
//                    PhraseTypeId = table.Column<Guid>(nullable: false),
//                    PhraseTypeText = table.Column<string>(nullable: true),
//                    Colour = table.Column<string>(nullable: true),
//                    ColourSyn = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_PhraseTypes", x => x.PhraseTypeId);
//                });

//            migrationBuilder.CreateTable(
//                name: "Statuss",
//                columns: table => new
//                {
//                    StatusId = table.Column<int>(nullable: false)
//                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
//                    StatusName = table.Column<string>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Statuss", x => x.StatusId);
//                });

//            migrationBuilder.CreateTable(
//                name: "AspNetRoleClaims",
//                columns: table => new
//                {
//                    Id = table.Column<int>(nullable: false)
//                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
//                    RoleId = table.Column<Guid>(nullable: false),
//                    ClaimType = table.Column<string>(nullable: true),
//                    ClaimValue = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
//                    table.ForeignKey(
//                        name: "FK_AspNetRoleClaims_AspNetRole_RoleId",
//                        column: x => x.RoleId,
//                        principalTable: "AspNetRole",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "Phrases",
//                columns: table => new
//                {
//                    PhraseId = table.Column<Guid>(nullable: false),
//                    PhraseText = table.Column<string>(nullable: true),
//                    PhraseTypeId = table.Column<Guid>(nullable: true),
//                    LanguageId = table.Column<int>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false),
//                    WordsSpace = table.Column<int>(nullable: true),
//                    Accurancy = table.Column<double>(nullable: true),
//                    IsTemplate = table.Column<bool>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Phrases", x => x.PhraseId);
//                    table.ForeignKey(
//                        name: "FK_Phrases_Languages_LanguageId",
//                        column: x => x.LanguageId,
//                        principalTable: "Languages",
//                        principalColumn: "LanguageId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Phrases_PhraseTypes_PhraseTypeId",
//                        column: x => x.PhraseTypeId,
//                        principalTable: "PhraseTypes",
//                        principalColumn: "PhraseTypeId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Companys",
//                columns: table => new
//                {
//                    CompanyId = table.Column<Guid>(nullable: false),
//                    CompanyName = table.Column<string>(nullable: false),
//                    CompanyIndustryId = table.Column<Guid>(nullable: true),
//                    CreationDate = table.Column<DateTime>(nullable: false),
//                    LanguageId = table.Column<int>(nullable: true),
//                    CountryId = table.Column<Guid>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    CorporationId = table.Column<Guid>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Companys", x => x.CompanyId);
//                    table.ForeignKey(
//                        name: "FK_Companys_CompanyIndustrys_CompanyIndustryId",
//                        column: x => x.CompanyIndustryId,
//                        principalTable: "CompanyIndustrys",
//                        principalColumn: "CompanyIndustryId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Companys_Corporations_CorporationId",
//                        column: x => x.CorporationId,
//                        principalTable: "Corporations",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Companys_Countrys_CountryId",
//                        column: x => x.CountryId,
//                        principalTable: "Countrys",
//                        principalColumn: "CountryId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Companys_Languages_LanguageId",
//                        column: x => x.LanguageId,
//                        principalTable: "Languages",
//                        principalColumn: "LanguageId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Companys_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "GoogleAccounts",
//                columns: table => new
//                {
//                    GoogleAccountId = table.Column<Guid>(nullable: false),
//                    GoogleKey = table.Column<string>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    CreationTime = table.Column<DateTime>(nullable: false),
//                    ExpirationTime = table.Column<DateTime>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_GoogleAccounts", x => x.GoogleAccountId);
//                    table.ForeignKey(
//                        name: "FK_GoogleAccounts_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Campaigns",
//                columns: table => new
//                {
//                    CampaignId = table.Column<Guid>(nullable: false),
//                    Name = table.Column<string>(nullable: true),
//                    IsSplash = table.Column<bool>(nullable: false),
//                    GenderId = table.Column<int>(nullable: false),
//                    BegAge = table.Column<int>(nullable: true),
//                    EndAge = table.Column<int>(nullable: true),
//                    BegDate = table.Column<DateTime>(nullable: true),
//                    EndDate = table.Column<DateTime>(nullable: true),
//                    CreationDate = table.Column<DateTime>(nullable: true),
//                    CompanyId = table.Column<Guid>(nullable: false),
//                    StatusId = table.Column<int>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Campaigns", x => x.CampaignId);
//                    table.ForeignKey(
//                        name: "FK_Campaigns_Companys_CompanyId",
//                        column: x => x.CompanyId,
//                        principalTable: "Companys",
//                        principalColumn: "CompanyId",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_Campaigns_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Contents",
//                columns: table => new
//                {
//                    ContentId = table.Column<Guid>(nullable: false),
//                    RawHTML = table.Column<string>(nullable: false),
//                    Name = table.Column<string>(nullable: true),
//                    Duration = table.Column<int>(nullable: false),
//                    CompanyId = table.Column<Guid>(nullable: true),
//                    JSONData = table.Column<string>(nullable: true),
//                    IsTemplate = table.Column<bool>(nullable: false),
//                    CreationDate = table.Column<DateTime>(nullable: true),
//                    UpdateDate = table.Column<DateTime>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Contents", x => x.ContentId);
//                    table.ForeignKey(
//                        name: "FK_Contents_Companys_CompanyId",
//                        column: x => x.CompanyId,
//                        principalTable: "Companys",
//                        principalColumn: "CompanyId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Payments",
//                columns: table => new
//                {
//                    PaymentId = table.Column<Guid>(nullable: false),
//                    CompanyId = table.Column<Guid>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    Date = table.Column<DateTime>(nullable: false),
//                    TransactionId = table.Column<string>(nullable: true),
//                    PaymentAmount = table.Column<double>(nullable: false),
//                    PaymentTime = table.Column<double>(nullable: false),
//                    PaymentComment = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
//                    table.ForeignKey(
//                        name: "FK_Payments_Companys_CompanyId",
//                        column: x => x.CompanyId,
//                        principalTable: "Companys",
//                        principalColumn: "CompanyId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Payments_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "PhraseCompanys",
//                columns: table => new
//                {
//                    PhraseCompanyId = table.Column<Guid>(nullable: false),
//                    PhraseId = table.Column<Guid>(nullable: true),
//                    CompanyId = table.Column<Guid>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_PhraseCompanys", x => x.PhraseCompanyId);
//                    table.ForeignKey(
//                        name: "FK_PhraseCompanys_Companys_CompanyId",
//                        column: x => x.CompanyId,
//                        principalTable: "Companys",
//                        principalColumn: "CompanyId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_PhraseCompanys_Phrases_PhraseId",
//                        column: x => x.PhraseId,
//                        principalTable: "Phrases",
//                        principalColumn: "PhraseId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Tariffs",
//                columns: table => new
//                {
//                    TariffId = table.Column<Guid>(nullable: false),
//                    CompanyId = table.Column<Guid>(nullable: true),
//                    CustomerKey = table.Column<string>(nullable: true),
//                    TotalRate = table.Column<decimal>(nullable: false),
//                    EmployeeNo = table.Column<int>(nullable: false),
//                    CreationDate = table.Column<DateTime>(nullable: false),
//                    ExpirationDate = table.Column<DateTime>(nullable: false),
//                    Rebillid = table.Column<string>(nullable: true),
//                    Token = table.Column<byte[]>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    isMonthly = table.Column<bool>(nullable: false),
//                    TariffComment = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Tariffs", x => x.TariffId);
//                    table.ForeignKey(
//                        name: "FK_Tariffs_Companys_CompanyId",
//                        column: x => x.CompanyId,
//                        principalTable: "Companys",
//                        principalColumn: "CompanyId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Tariffs_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "WorkerTypes",
//                columns: table => new
//                {
//                    WorkerTypeId = table.Column<Guid>(nullable: false),
//                    CompanyId = table.Column<Guid>(nullable: false),
//                    WorkerTypeName = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_WorkerTypes", x => x.WorkerTypeId);
//                    table.ForeignKey(
//                        name: "FK_WorkerTypes_Companys_CompanyId",
//                        column: x => x.CompanyId,
//                        principalTable: "Companys",
//                        principalColumn: "CompanyId",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "CampaignContents",
//                columns: table => new
//                {
//                    CampaignContentId = table.Column<Guid>(nullable: false),
//                    SequenceNumber = table.Column<int>(nullable: false),
//                    ContentId = table.Column<Guid>(nullable: true),
//                    CampaignId = table.Column<Guid>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_CampaignContents", x => x.CampaignContentId);
//                    table.ForeignKey(
//                        name: "FK_CampaignContents_Campaigns_CampaignId",
//                        column: x => x.CampaignId,
//                        principalTable: "Campaigns",
//                        principalColumn: "CampaignId",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_CampaignContents_Contents_ContentId",
//                        column: x => x.ContentId,
//                        principalTable: "Contents",
//                        principalColumn: "ContentId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Transactions",
//                columns: table => new
//                {
//                    TransactionId = table.Column<Guid>(nullable: false),
//                    Amount = table.Column<decimal>(nullable: false),
//                    OrderId = table.Column<string>(nullable: true),
//                    PaymentId = table.Column<string>(nullable: true),
//                    TariffId = table.Column<Guid>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    PaymentDate = table.Column<DateTime>(nullable: false),
//                    TransactionComment = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
//                    table.ForeignKey(
//                        name: "FK_Transactions_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Transactions_Tariffs_TariffId",
//                        column: x => x.TariffId,
//                        principalTable: "Tariffs",
//                        principalColumn: "TariffId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "AspNetUsers",
//                columns: table => new
//                {
//                    Id = table.Column<Guid>(nullable: false),
//                    UserName = table.Column<string>(maxLength: 256, nullable: true),
//                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
//                    Email = table.Column<string>(maxLength: 256, nullable: true),
//                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
//                    EmailConfirmed = table.Column<bool>(nullable: false),
//                    PasswordHash = table.Column<string>(nullable: true),
//                    SecurityStamp = table.Column<string>(nullable: true),
//                    ConcurrencyStamp = table.Column<string>(nullable: true),
//                    PhoneNumber = table.Column<string>(nullable: true),
//                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
//                    TwoFactorEnabled = table.Column<bool>(nullable: false),
//                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
//                    LockoutEnabled = table.Column<bool>(nullable: false),
//                    AccessFailedCount = table.Column<int>(nullable: false),
//                    FullName = table.Column<string>(nullable: true),
//                    Avatar = table.Column<string>(nullable: true),
//                    EmpoyeeId = table.Column<string>(nullable: true),
//                    CreationDate = table.Column<DateTime>(nullable: false),
//                    CompanyId = table.Column<Guid>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    OneSignalId = table.Column<string>(nullable: true),
//                    WorkerTypeId = table.Column<Guid>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
//                    table.ForeignKey(
//                        name: "FK_AspNetUsers_Companys_CompanyId",
//                        column: x => x.CompanyId,
//                        principalTable: "Companys",
//                        principalColumn: "CompanyId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_AspNetUsers_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_AspNetUsers_WorkerTypes_WorkerTypeId",
//                        column: x => x.WorkerTypeId,
//                        principalTable: "WorkerTypes",
//                        principalColumn: "WorkerTypeId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "CampaignContentAnswers",
//                columns: table => new
//                {
//                    CampaignContentAnswerId = table.Column<Guid>(nullable: false),
//                    Answer = table.Column<string>(nullable: true),
//                    CampaignContentId = table.Column<Guid>(nullable: false),
//                    Time = table.Column<DateTime>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_CampaignContentAnswers", x => x.CampaignContentAnswerId);
//                    table.ForeignKey(
//                        name: "FK_CampaignContentAnswers_CampaignContents_CampaignContentId",
//                        column: x => x.CampaignContentId,
//                        principalTable: "CampaignContents",
//                        principalColumn: "CampaignContentId",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "AspNetUserClaims",
//                columns: table => new
//                {
//                    Id = table.Column<int>(nullable: false)
//                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
//                    UserId = table.Column<Guid>(nullable: false),
//                    ClaimType = table.Column<string>(nullable: true),
//                    ClaimValue = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
//                    table.ForeignKey(
//                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
//                        column: x => x.UserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "AspNetUserLogins",
//                columns: table => new
//                {
//                    LoginProvider = table.Column<string>(nullable: false),
//                    ProviderKey = table.Column<string>(nullable: false),
//                    ProviderDisplayName = table.Column<string>(nullable: true),
//                    UserId = table.Column<Guid>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.ProviderKey, x.LoginProvider });
//                    table.UniqueConstraint("AK_AspNetUserLogins_LoginProvider_ProviderKey", x => new { x.LoginProvider, x.ProviderKey });
//                    table.ForeignKey(
//                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
//                        column: x => x.UserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "AspNetUserRoles",
//                columns: table => new
//                {
//                    UserId = table.Column<Guid>(nullable: false),
//                    RoleId = table.Column<Guid>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
//                    table.ForeignKey(
//                        name: "FK_AspNetUserRoles_AspNetRole_RoleId",
//                        column: x => x.RoleId,
//                        principalTable: "AspNetRole",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
//                        column: x => x.UserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "AspNetUserTokens",
//                columns: table => new
//                {
//                    UserId = table.Column<Guid>(nullable: false),
//                    LoginProvider = table.Column<string>(nullable: false),
//                    Name = table.Column<string>(nullable: false),
//                    Value = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
//                    table.ForeignKey(
//                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
//                        column: x => x.UserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueMarkups",
//                columns: table => new
//                {
//                    DialogueMarkUpId = table.Column<Guid>(nullable: false),
//                    ApplicationUserId = table.Column<Guid>(nullable: true),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    CreationTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    BegTimeMarkup = table.Column<DateTime>(nullable: false),
//                    EndTimeMarkup = table.Column<DateTime>(nullable: false),
//                    IsDialogue = table.Column<bool>(nullable: false),
//                    StatusId = table.Column<int>(nullable: true),
//                    TeacherId = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueMarkups", x => x.DialogueMarkUpId);
//                    table.ForeignKey(
//                        name: "FK_DialogueMarkups_AspNetUsers_ApplicationUserId",
//                        column: x => x.ApplicationUserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_DialogueMarkups_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Dialogues",
//                columns: table => new
//                {
//                    DialogueId = table.Column<Guid>(nullable: false),
//                    CreationTime = table.Column<DateTime>(nullable: false),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    ApplicationUserId = table.Column<Guid>(nullable: false),
//                    LanguageId = table.Column<int>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    SysVersion = table.Column<string>(nullable: true),
//                    InStatistic = table.Column<bool>(nullable: false),
//                    Comment = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Dialogues", x => x.DialogueId);
//                    table.ForeignKey(
//                        name: "FK_Dialogues_AspNetUsers_ApplicationUserId",
//                        column: x => x.ApplicationUserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_Dialogues_Languages_LanguageId",
//                        column: x => x.LanguageId,
//                        principalTable: "Languages",
//                        principalColumn: "LanguageId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_Dialogues_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "FileAudioEmployees",
//                columns: table => new
//                {
//                    FileAudioEmployeeId = table.Column<Guid>(nullable: false),
//                    ApplicationUserId = table.Column<Guid>(nullable: false),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    CreationTime = table.Column<DateTime>(nullable: false),
//                    FileName = table.Column<string>(nullable: true),
//                    FileContainer = table.Column<string>(nullable: true),
//                    FileExist = table.Column<bool>(nullable: false),
//                    StatusId = table.Column<int>(nullable: true),
//                    Duration = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_FileAudioEmployees", x => x.FileAudioEmployeeId);
//                    table.ForeignKey(
//                        name: "FK_FileAudioEmployees_AspNetUsers_ApplicationUserId",
//                        column: x => x.ApplicationUserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_FileAudioEmployees_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "FileFrames",
//                columns: table => new
//                {
//                    FileFrameId = table.Column<Guid>(nullable: false),
//                    ApplicationUserId = table.Column<Guid>(nullable: false),
//                    FileExist = table.Column<bool>(nullable: false),
//                    FileName = table.Column<string>(nullable: true),
//                    FileContainer = table.Column<string>(nullable: true),
//                    StatusId = table.Column<int>(nullable: true),
//                    StatusNNId = table.Column<int>(nullable: true),
//                    Time = table.Column<DateTime>(nullable: false),
//                    FaceId = table.Column<Guid>(nullable: true),
//                    IsFacePresent = table.Column<bool>(nullable: false),
//                    FaceLength = table.Column<int>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_FileFrames", x => x.FileFrameId);
//                    table.ForeignKey(
//                        name: "FK_FileFrames_AspNetUsers_ApplicationUserId",
//                        column: x => x.ApplicationUserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_FileFrames_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_FileFrames_Statuss_StatusNNId",
//                        column: x => x.StatusNNId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "FileVideos",
//                columns: table => new
//                {
//                    FileVideoId = table.Column<Guid>(nullable: false),
//                    ApplicationUserId = table.Column<Guid>(nullable: false),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    CreationTime = table.Column<DateTime>(nullable: false),
//                    FileName = table.Column<string>(nullable: true),
//                    FileContainer = table.Column<string>(nullable: true),
//                    FileExist = table.Column<bool>(nullable: false),
//                    StatusId = table.Column<int>(nullable: true),
//                    Duration = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_FileVideos", x => x.FileVideoId);
//                    table.ForeignKey(
//                        name: "FK_FileVideos_AspNetUsers_ApplicationUserId",
//                        column: x => x.ApplicationUserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_FileVideos_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "Sessions",
//                columns: table => new
//                {
//                    SessionId = table.Column<Guid>(nullable: false),
//                    ApplicationUserId = table.Column<Guid>(nullable: false),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    StatusId = table.Column<int>(nullable: true),
//                    IsDesktop = table.Column<bool>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
//                    table.ForeignKey(
//                        name: "FK_Sessions_AspNetUsers_ApplicationUserId",
//                        column: x => x.ApplicationUserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_Sessions_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "SlideShowSessions",
//                columns: table => new
//                {
//                    SlideShowSessionId = table.Column<Guid>(nullable: false),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    CampaignContentId = table.Column<Guid>(nullable: true),
//                    ContentType = table.Column<string>(nullable: true),
//                    ApplicationUserId = table.Column<Guid>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_SlideShowSessions", x => x.SlideShowSessionId);
//                    table.ForeignKey(
//                        name: "FK_SlideShowSessions_AspNetUsers_ApplicationUserId",
//                        column: x => x.ApplicationUserId,
//                        principalTable: "AspNetUsers",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_SlideShowSessions_CampaignContents_CampaignContentId",
//                        column: x => x.CampaignContentId,
//                        principalTable: "CampaignContents",
//                        principalColumn: "CampaignContentId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueAudios",
//                columns: table => new
//                {
//                    DialogueAudioId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false),
//                    NeutralityTone = table.Column<double>(nullable: true),
//                    PositiveTone = table.Column<double>(nullable: true),
//                    NegativeTone = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueAudios", x => x.DialogueAudioId);
//                    table.ForeignKey(
//                        name: "FK_DialogueAudios_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueClientProfiles",
//                columns: table => new
//                {
//                    DialogueClientProfileId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false),
//                    Avatar = table.Column<string>(nullable: true),
//                    Age = table.Column<double>(nullable: true),
//                    Gender = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueClientProfiles", x => x.DialogueClientProfileId);
//                    table.ForeignKey(
//                        name: "FK_DialogueClientProfiles_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueClientSatisfactions",
//                columns: table => new
//                {
//                    DialogueClientSatisfactionId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    MeetingExpectationsTotal = table.Column<double>(nullable: true),
//                    BegMoodTotal = table.Column<double>(nullable: true),
//                    EndMoodTotal = table.Column<double>(nullable: true),
//                    MeetingExpectationsByClient = table.Column<double>(nullable: true),
//                    MeetingExpectationsByEmpoyee = table.Column<double>(nullable: true),
//                    BegMoodByEmpoyee = table.Column<double>(nullable: true),
//                    EndMoodByEmpoyee = table.Column<double>(nullable: true),
//                    MeetingExpectationsByTeacher = table.Column<double>(nullable: true),
//                    BegMoodByTeacher = table.Column<double>(nullable: true),
//                    EndMoodByTeacher = table.Column<double>(nullable: true),
//                    MeetingExpectationsByNN = table.Column<double>(nullable: true),
//                    BegMoodByNN = table.Column<double>(nullable: true),
//                    EndMoodByNN = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueClientSatisfactions", x => x.DialogueClientSatisfactionId);
//                    table.ForeignKey(
//                        name: "FK_DialogueClientSatisfactions_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueFrames",
//                columns: table => new
//                {
//                    DialogueFrameId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false),
//                    Time = table.Column<DateTime>(nullable: false),
//                    HappinessShare = table.Column<double>(nullable: true),
//                    NeutralShare = table.Column<double>(nullable: true),
//                    SurpriseShare = table.Column<double>(nullable: true),
//                    SadnessShare = table.Column<double>(nullable: true),
//                    AngerShare = table.Column<double>(nullable: true),
//                    DisgustShare = table.Column<double>(nullable: true),
//                    ContemptShare = table.Column<double>(nullable: true),
//                    FearShare = table.Column<double>(nullable: true),
//                    YawShare = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueFrames", x => x.DialogueFrameId);
//                    table.ForeignKey(
//                        name: "FK_DialogueFrames_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueHints",
//                columns: table => new
//                {
//                    DialogueHintId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    HintText = table.Column<string>(nullable: true),
//                    IsAutomatic = table.Column<bool>(nullable: false),
//                    Type = table.Column<string>(nullable: true),
//                    IsPositive = table.Column<bool>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueHints", x => x.DialogueHintId);
//                    table.ForeignKey(
//                        name: "FK_DialogueHints_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueIntervals",
//                columns: table => new
//                {
//                    DialogueIntervalId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    NeutralityTone = table.Column<double>(nullable: true),
//                    HappinessTone = table.Column<double>(nullable: true),
//                    SadnessTone = table.Column<double>(nullable: true),
//                    AngerTone = table.Column<double>(nullable: true),
//                    FearTone = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueIntervals", x => x.DialogueIntervalId);
//                    table.ForeignKey(
//                        name: "FK_DialogueIntervals_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialoguePhraseCounts",
//                columns: table => new
//                {
//                    DialoguePhraseCountId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    PhraseTypeId = table.Column<Guid>(nullable: true),
//                    PhraseCount = table.Column<int>(nullable: false),
//                    IsClient = table.Column<bool>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialoguePhraseCounts", x => x.DialoguePhraseCountId);
//                    table.ForeignKey(
//                        name: "FK_DialoguePhraseCounts_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_DialoguePhraseCounts_PhraseTypes_PhraseTypeId",
//                        column: x => x.PhraseTypeId,
//                        principalTable: "PhraseTypes",
//                        principalColumn: "PhraseTypeId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialoguePhrases",
//                columns: table => new
//                {
//                    DialoguePhraseId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    PhraseTypeId = table.Column<Guid>(nullable: true),
//                    PhraseId = table.Column<Guid>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialoguePhrases", x => x.DialoguePhraseId);
//                    table.ForeignKey(
//                        name: "FK_DialoguePhrases_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_DialoguePhrases_Phrases_PhraseId",
//                        column: x => x.PhraseId,
//                        principalTable: "Phrases",
//                        principalColumn: "PhraseId",
//                        onDelete: ReferentialAction.Restrict);
//                    table.ForeignKey(
//                        name: "FK_DialoguePhrases_PhraseTypes_PhraseTypeId",
//                        column: x => x.PhraseTypeId,
//                        principalTable: "PhraseTypes",
//                        principalColumn: "PhraseTypeId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueSpeechs",
//                columns: table => new
//                {
//                    DialogueSpeechId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false),
//                    PositiveShare = table.Column<double>(nullable: true),
//                    SpeechSpeed = table.Column<double>(nullable: true),
//                    SilenceShare = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueSpeechs", x => x.DialogueSpeechId);
//                    table.ForeignKey(
//                        name: "FK_DialogueSpeechs_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueVisuals",
//                columns: table => new
//                {
//                    DialogueVisualId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    AttentionShare = table.Column<double>(nullable: true),
//                    HappinessShare = table.Column<double>(nullable: true),
//                    NeutralShare = table.Column<double>(nullable: true),
//                    SurpriseShare = table.Column<double>(nullable: true),
//                    SadnessShare = table.Column<double>(nullable: true),
//                    AngerShare = table.Column<double>(nullable: true),
//                    DisgustShare = table.Column<double>(nullable: true),
//                    ContemptShare = table.Column<double>(nullable: true),
//                    FearShare = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueVisuals", x => x.DialogueVisualId);
//                    table.ForeignKey(
//                        name: "FK_DialogueVisuals_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "DialogueWords",
//                columns: table => new
//                {
//                    DialogueWordId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: true),
//                    IsClient = table.Column<bool>(nullable: false),
//                    Words = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_DialogueWords", x => x.DialogueWordId);
//                    table.ForeignKey(
//                        name: "FK_DialogueWords_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "FileAudioDialogues",
//                columns: table => new
//                {
//                    FileAudioDialogueId = table.Column<Guid>(nullable: false),
//                    DialogueId = table.Column<Guid>(nullable: false),
//                    BegTime = table.Column<DateTime>(nullable: false),
//                    EndTime = table.Column<DateTime>(nullable: false),
//                    CreationTime = table.Column<DateTime>(nullable: false),
//                    TransactionId = table.Column<string>(nullable: true),
//                    FileName = table.Column<string>(nullable: true),
//                    FileContainer = table.Column<string>(nullable: true),
//                    FileExist = table.Column<bool>(nullable: false),
//                    StatusId = table.Column<int>(nullable: true),
//                    Duration = table.Column<double>(nullable: true),
//                    STTResult = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_FileAudioDialogues", x => x.FileAudioDialogueId);
//                    table.ForeignKey(
//                        name: "FK_FileAudioDialogues_Dialogues_DialogueId",
//                        column: x => x.DialogueId,
//                        principalTable: "Dialogues",
//                        principalColumn: "DialogueId",
//                        onDelete: ReferentialAction.Cascade);
//                    table.ForeignKey(
//                        name: "FK_FileAudioDialogues_Statuss_StatusId",
//                        column: x => x.StatusId,
//                        principalTable: "Statuss",
//                        principalColumn: "StatusId",
//                        onDelete: ReferentialAction.Restrict);
//                });

//            migrationBuilder.CreateTable(
//                name: "FrameAttributes",
//                columns: table => new
//                {
//                    FrameAttributeId = table.Column<Guid>(nullable: false),
//                    FileFrameId = table.Column<Guid>(nullable: false),
//                    Gender = table.Column<string>(nullable: true),
//                    Age = table.Column<double>(nullable: false),
//                    Value = table.Column<string>(nullable: true),
//                    Descriptor = table.Column<string>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_FrameAttributes", x => x.FrameAttributeId);
//                    table.ForeignKey(
//                        name: "FK_FrameAttributes_FileFrames_FileFrameId",
//                        column: x => x.FileFrameId,
//                        principalTable: "FileFrames",
//                        principalColumn: "FileFrameId",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "FrameEmotions",
//                columns: table => new
//                {
//                    FrameEmotionId = table.Column<Guid>(nullable: false),
//                    FileFrameId = table.Column<Guid>(nullable: false),
//                    AngerShare = table.Column<double>(nullable: true),
//                    ContemptShare = table.Column<double>(nullable: true),
//                    DisgustShare = table.Column<double>(nullable: true),
//                    HappinessShare = table.Column<double>(nullable: true),
//                    NeutralShare = table.Column<double>(nullable: true),
//                    SadnessShare = table.Column<double>(nullable: true),
//                    SurpriseShare = table.Column<double>(nullable: true),
//                    FearShare = table.Column<double>(nullable: true),
//                    YawShare = table.Column<double>(nullable: true)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_FrameEmotions", x => x.FrameEmotionId);
//                    table.ForeignKey(
//                        name: "FK_FrameEmotions_FileFrames_FileFrameId",
//                        column: x => x.FileFrameId,
//                        principalTable: "FileFrames",
//                        principalColumn: "FileFrameId",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateIndex(
//                name: "RoleNameIndex",
//                table: "AspNetRole",
//                column: "NormalizedName",
//                unique: true);

//            migrationBuilder.CreateIndex(
//                name: "IX_AspNetRoleClaims_RoleId",
//                table: "AspNetRoleClaims",
//                column: "RoleId");

//            migrationBuilder.CreateIndex(
//                name: "IX_AspNetUserClaims_UserId",
//                table: "AspNetUserClaims",
//                column: "UserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_AspNetUserLogins_UserId",
//                table: "AspNetUserLogins",
//                column: "UserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_AspNetUserRoles_RoleId",
//                table: "AspNetUserRoles",
//                column: "RoleId");

//            migrationBuilder.CreateIndex(
//                name: "IX_AspNetUsers_CompanyId",
//                table: "AspNetUsers",
//                column: "CompanyId");

//            migrationBuilder.CreateIndex(
//                name: "EmailIndex",
//                table: "AspNetUsers",
//                column: "NormalizedEmail");

//            migrationBuilder.CreateIndex(
//                name: "UserNameIndex",
//                table: "AspNetUsers",
//                column: "NormalizedUserName",
//                unique: true);

//            migrationBuilder.CreateIndex(
//                name: "IX_AspNetUsers_StatusId",
//                table: "AspNetUsers",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_AspNetUsers_WorkerTypeId",
//                table: "AspNetUsers",
//                column: "WorkerTypeId");

//            migrationBuilder.CreateIndex(
//                name: "IX_CampaignContentAnswers_CampaignContentId",
//                table: "CampaignContentAnswers",
//                column: "CampaignContentId");

//            migrationBuilder.CreateIndex(
//                name: "IX_CampaignContents_CampaignId",
//                table: "CampaignContents",
//                column: "CampaignId");

//            migrationBuilder.CreateIndex(
//                name: "IX_CampaignContents_ContentId",
//                table: "CampaignContents",
//                column: "ContentId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Campaigns_CompanyId",
//                table: "Campaigns",
//                column: "CompanyId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Campaigns_StatusId",
//                table: "Campaigns",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Companys_CompanyIndustryId",
//                table: "Companys",
//                column: "CompanyIndustryId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Companys_CorporationId",
//                table: "Companys",
//                column: "CorporationId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Companys_CountryId",
//                table: "Companys",
//                column: "CountryId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Companys_LanguageId",
//                table: "Companys",
//                column: "LanguageId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Companys_StatusId",
//                table: "Companys",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Contents_CompanyId",
//                table: "Contents",
//                column: "CompanyId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueAudios_DialogueId",
//                table: "DialogueAudios",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueClientProfiles_DialogueId",
//                table: "DialogueClientProfiles",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueClientSatisfactions_DialogueId",
//                table: "DialogueClientSatisfactions",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueFrames_DialogueId",
//                table: "DialogueFrames",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueHints_DialogueId",
//                table: "DialogueHints",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueIntervals_DialogueId",
//                table: "DialogueIntervals",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueMarkups_ApplicationUserId",
//                table: "DialogueMarkups",
//                column: "ApplicationUserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueMarkups_StatusId",
//                table: "DialogueMarkups",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialoguePhraseCounts_DialogueId",
//                table: "DialoguePhraseCounts",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialoguePhraseCounts_PhraseTypeId",
//                table: "DialoguePhraseCounts",
//                column: "PhraseTypeId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialoguePhrases_DialogueId",
//                table: "DialoguePhrases",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialoguePhrases_PhraseId",
//                table: "DialoguePhrases",
//                column: "PhraseId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialoguePhrases_PhraseTypeId",
//                table: "DialoguePhrases",
//                column: "PhraseTypeId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Dialogues_ApplicationUserId",
//                table: "Dialogues",
//                column: "ApplicationUserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Dialogues_LanguageId",
//                table: "Dialogues",
//                column: "LanguageId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Dialogues_StatusId",
//                table: "Dialogues",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueSpeechs_DialogueId",
//                table: "DialogueSpeechs",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueVisuals_DialogueId",
//                table: "DialogueVisuals",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_DialogueWords_DialogueId",
//                table: "DialogueWords",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileAudioDialogues_DialogueId",
//                table: "FileAudioDialogues",
//                column: "DialogueId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileAudioDialogues_StatusId",
//                table: "FileAudioDialogues",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileAudioEmployees_ApplicationUserId",
//                table: "FileAudioEmployees",
//                column: "ApplicationUserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileAudioEmployees_StatusId",
//                table: "FileAudioEmployees",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileFrames_ApplicationUserId",
//                table: "FileFrames",
//                column: "ApplicationUserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileFrames_StatusId",
//                table: "FileFrames",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileFrames_StatusNNId",
//                table: "FileFrames",
//                column: "StatusNNId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileVideos_ApplicationUserId",
//                table: "FileVideos",
//                column: "ApplicationUserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FileVideos_StatusId",
//                table: "FileVideos",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FrameAttributes_FileFrameId",
//                table: "FrameAttributes",
//                column: "FileFrameId");

//            migrationBuilder.CreateIndex(
//                name: "IX_FrameEmotions_FileFrameId",
//                table: "FrameEmotions",
//                column: "FileFrameId");

//            migrationBuilder.CreateIndex(
//                name: "IX_GoogleAccounts_StatusId",
//                table: "GoogleAccounts",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Payments_CompanyId",
//                table: "Payments",
//                column: "CompanyId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Payments_StatusId",
//                table: "Payments",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_PhraseCompanys_CompanyId",
//                table: "PhraseCompanys",
//                column: "CompanyId");

//            migrationBuilder.CreateIndex(
//                name: "IX_PhraseCompanys_PhraseId",
//                table: "PhraseCompanys",
//                column: "PhraseId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Phrases_LanguageId",
//                table: "Phrases",
//                column: "LanguageId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Phrases_PhraseTypeId",
//                table: "Phrases",
//                column: "PhraseTypeId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Sessions_ApplicationUserId",
//                table: "Sessions",
//                column: "ApplicationUserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Sessions_StatusId",
//                table: "Sessions",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_SlideShowSessions_ApplicationUserId",
//                table: "SlideShowSessions",
//                column: "ApplicationUserId");

//            migrationBuilder.CreateIndex(
//                name: "IX_SlideShowSessions_CampaignContentId",
//                table: "SlideShowSessions",
//                column: "CampaignContentId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Tariffs_CompanyId",
//                table: "Tariffs",
//                column: "CompanyId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Tariffs_StatusId",
//                table: "Tariffs",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Transactions_StatusId",
//                table: "Transactions",
//                column: "StatusId");

//            migrationBuilder.CreateIndex(
//                name: "IX_Transactions_TariffId",
//                table: "Transactions",
//                column: "TariffId");

//            migrationBuilder.CreateIndex(
//                name: "IX_WorkerTypes_CompanyId",
//                table: "WorkerTypes",
//                column: "CompanyId");
//        }

//        protected override void Down(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.DropTable(
//                name: "AspNetRoleClaims");

//            migrationBuilder.DropTable(
//                name: "AspNetUserClaims");

//            migrationBuilder.DropTable(
//                name: "AspNetUserLogins");

//            migrationBuilder.DropTable(
//                name: "AspNetUserRoles");

//            migrationBuilder.DropTable(
//                name: "AspNetUserTokens");

//            migrationBuilder.DropTable(
//                name: "CampaignContentAnswers");

//            migrationBuilder.DropTable(
//                name: "CatalogueHints");

//            migrationBuilder.DropTable(
//                name: "DialogueAudios");

//            migrationBuilder.DropTable(
//                name: "DialogueClientProfiles");

//            migrationBuilder.DropTable(
//                name: "DialogueClientSatisfactions");

//            migrationBuilder.DropTable(
//                name: "DialogueFrames");

//            migrationBuilder.DropTable(
//                name: "DialogueHints");

//            migrationBuilder.DropTable(
//                name: "DialogueIntervals");

//            migrationBuilder.DropTable(
//                name: "DialogueMarkups");

//            migrationBuilder.DropTable(
//                name: "DialoguePhraseCounts");

//            migrationBuilder.DropTable(
//                name: "DialoguePhrases");

//            migrationBuilder.DropTable(
//                name: "DialogueSpeechs");

//            migrationBuilder.DropTable(
//                name: "DialogueVisuals");

//            migrationBuilder.DropTable(
//                name: "DialogueWords");

//            migrationBuilder.DropTable(
//                name: "FileAudioDialogues");

//            migrationBuilder.DropTable(
//                name: "FileAudioEmployees");

//            migrationBuilder.DropTable(
//                name: "FileVideos");

//            migrationBuilder.DropTable(
//                name: "FrameAttributes");

//            migrationBuilder.DropTable(
//                name: "FrameEmotions");

//            migrationBuilder.DropTable(
//                name: "GoogleAccounts");

//            migrationBuilder.DropTable(
//                name: "Payments");

//            migrationBuilder.DropTable(
//                name: "PhraseCompanys");

//            migrationBuilder.DropTable(
//                name: "Sessions");

//            migrationBuilder.DropTable(
//                name: "SlideShowSessions");

//            migrationBuilder.DropTable(
//                name: "Transactions");

//            migrationBuilder.DropTable(
//                name: "AspNetRole");

//            migrationBuilder.DropTable(
//                name: "Dialogues");

//            migrationBuilder.DropTable(
//                name: "FileFrames");

//            migrationBuilder.DropTable(
//                name: "Phrases");

//            migrationBuilder.DropTable(
//                name: "CampaignContents");

//            migrationBuilder.DropTable(
//                name: "Tariffs");

//            migrationBuilder.DropTable(
//                name: "AspNetUsers");

//            migrationBuilder.DropTable(
//                name: "PhraseTypes");

//            migrationBuilder.DropTable(
//                name: "Campaigns");

//            migrationBuilder.DropTable(
//                name: "Contents");

//            migrationBuilder.DropTable(
//                name: "WorkerTypes");

//            migrationBuilder.DropTable(
//                name: "Companys");

//            migrationBuilder.DropTable(
//                name: "CompanyIndustrys");

//            migrationBuilder.DropTable(
//                name: "Corporations");

//            migrationBuilder.DropTable(
//                name: "Countrys");

//            migrationBuilder.DropTable(
//                name: "Languages");

//            migrationBuilder.DropTable(
//                name: "Statuss");
//        }
//    }
//}
