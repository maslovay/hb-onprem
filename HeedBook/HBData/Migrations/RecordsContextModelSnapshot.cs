﻿// <auto-generated />
using System;
using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HBData.Migrations
{
    [DbContext(typeof(RecordsContext))]
    partial class RecordsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("HBData.Models.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Avatar");

                    b.Property<int?>("CompanyId");

                    b.Property<DateTime>("CreationDate");

                    b.Property<string>("Email");

                    b.Property<string>("EmployeeId");

                    b.Property<string>("FullName");

                    b.Property<string>("OneSignalId");

                    b.Property<int?>("StatusId");

                    b.Property<int?>("WorkerTypeId");

                    b.HasKey("Id");

                    b.HasIndex("CompanyId");

                    b.HasIndex("StatusId");

                    b.HasIndex("WorkerTypeId");

                    b.ToTable("ApplicationUsers");
                });

            modelBuilder.Entity("HBData.Models.AspNetRole", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp");

                    b.Property<string>("Name");

                    b.Property<string>("NormalizedName");

                    b.HasKey("Id");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("HBData.Models.AspNetUserRole", b =>
                {
                    b.Property<string>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("RoleId");

                    b.HasKey("UserId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("HBData.Models.Campaign", b =>
                {
                    b.Property<Guid>("CampaignId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BegAge");

                    b.Property<DateTime?>("BegDate");

                    b.Property<int>("CompanyId");

                    b.Property<DateTime?>("CreationDate");

                    b.Property<int?>("EndAge");

                    b.Property<DateTime?>("EndDate");

                    b.Property<int>("GenderId");

                    b.Property<bool>("IsSplash");

                    b.Property<string>("Name");

                    b.Property<int?>("StatusId");

                    b.HasKey("CampaignId");

                    b.HasIndex("CompanyId");

                    b.HasIndex("StatusId");

                    b.ToTable("Campaigns");
                });

            modelBuilder.Entity("HBData.Models.CampaignContent", b =>
                {
                    b.Property<Guid>("CampaignContentId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("CampaignId");

                    b.Property<Guid?>("ContentId");

                    b.Property<int>("SequenceNumber");

                    b.HasKey("CampaignContentId");

                    b.HasIndex("CampaignId");

                    b.HasIndex("ContentId");

                    b.ToTable("CampaignContents");
                });

            modelBuilder.Entity("HBData.Models.CampaignContentSession", b =>
                {
                    b.Property<Guid>("CampaignContentSessionId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ApplicationUserId");

                    b.Property<DateTime>("BegTime");

                    b.Property<Guid>("CampaignContentId");

                    b.HasKey("CampaignContentSessionId");

                    b.HasIndex("ApplicationUserId");

                    b.HasIndex("CampaignContentId");

                    b.ToTable("CampaignContentSessions");
                });

            modelBuilder.Entity("HBData.Models.Company", b =>
                {
                    b.Property<int>("CompanyId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("CompanyIndustryId");

                    b.Property<string>("CompanyName")
                        .IsRequired();

                    b.Property<int?>("CorporationId");

                    b.Property<int?>("CountryId");

                    b.Property<DateTime>("CreationDate");

                    b.Property<int>("LanguageId");

                    b.Property<int?>("StatusId");

                    b.HasKey("CompanyId");

                    b.HasIndex("CompanyIndustryId");

                    b.HasIndex("CorporationId");

                    b.HasIndex("CountryId");

                    b.HasIndex("LanguageId");

                    b.HasIndex("StatusId");

                    b.ToTable("Companies");
                });

            modelBuilder.Entity("HBData.Models.CompanyIndustry", b =>
                {
                    b.Property<int>("CompanyIndustryId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CompanyIndustryName");

                    b.Property<double?>("CrossSalesIndex");

                    b.Property<double?>("LoadIndex");

                    b.Property<double?>("SatisfactionIndex");

                    b.HasKey("CompanyIndustryId");

                    b.ToTable("CompanyIndustries");
                });

            modelBuilder.Entity("HBData.Models.Content", b =>
                {
                    b.Property<Guid>("ContentId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CompanyId");

                    b.Property<DateTime?>("CreationDate");

                    b.Property<int>("Duration");

                    b.Property<bool>("IsTemplate");

                    b.Property<string>("JSONData");

                    b.Property<string>("Name");

                    b.Property<string>("RawHTML")
                        .IsRequired();

                    b.Property<DateTime?>("UpdateDate");

                    b.HasKey("ContentId");

                    b.HasIndex("CompanyId");

                    b.ToTable("Contents");
                });

            modelBuilder.Entity("HBData.Models.Corporation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Corporations");
                });

            modelBuilder.Entity("HBData.Models.Country", b =>
                {
                    b.Property<int>("CountryId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CountryName");

                    b.HasKey("CountryId");

                    b.ToTable("Countries");
                });

            modelBuilder.Entity("HBData.Models.DialogueAudio", b =>
                {
                    b.Property<Guid>("DialogueAudioId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid?>("DialogueId");

                    b.Property<bool>("IsClient");

                    b.Property<double?>("NegativeTone");

                    b.Property<double?>("NeutralityTone");

                    b.Property<double?>("PositiveTone");

                    b.HasKey("DialogueAudioId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueAudios");
                });

            modelBuilder.Entity("HBData.Models.DialogueClientProfile", b =>
                {
                    b.Property<Guid>("DialogueClientProfileId")
                        .ValueGeneratedOnAdd();

                    b.Property<double?>("Age");

                    b.Property<string>("Avatar");

                    b.Property<Guid?>("DialogueId");

                    b.Property<string>("Gender");

                    b.Property<bool>("IsClient");

                    b.HasKey("DialogueClientProfileId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueClientProfiles");
                });

            modelBuilder.Entity("HBData.Models.DialogueClientSatisfaction", b =>
                {
                    b.Property<Guid>("DialogueClientSatisfactionId")
                        .ValueGeneratedOnAdd();

                    b.Property<double?>("BegMoodByEmpoyee");

                    b.Property<double?>("BegMoodByNN");

                    b.Property<double?>("BegMoodByTeacher");

                    b.Property<double?>("BegMoodTotal");

                    b.Property<Guid?>("DialogueId");

                    b.Property<double?>("EndMoodByEmpoyee");

                    b.Property<double?>("EndMoodByNN");

                    b.Property<double?>("EndMoodByTeacher");

                    b.Property<double?>("EndMoodTotal");

                    b.Property<double?>("MeetingExpectationsByClient");

                    b.Property<double?>("MeetingExpectationsByEmpoyee");

                    b.Property<double?>("MeetingExpectationsByNN");

                    b.Property<double?>("MeetingExpectationsByTeacher");

                    b.Property<double?>("MeetingExpectationsTotal");

                    b.HasKey("DialogueClientSatisfactionId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueClientSatisfactions");
                });

            modelBuilder.Entity("HBData.Models.DialogueFrame", b =>
                {
                    b.Property<Guid>("DialogueFrameId")
                        .ValueGeneratedOnAdd();

                    b.Property<double?>("AngerShare");

                    b.Property<double?>("ContemptShare");

                    b.Property<Guid?>("DialogueId");

                    b.Property<double?>("DisgustShare");

                    b.Property<double?>("FearShare");

                    b.Property<double?>("HappinessShare");

                    b.Property<bool>("IsClient");

                    b.Property<double?>("NeutralShare");

                    b.Property<double?>("SadnessShare");

                    b.Property<double?>("SurpriseShare");

                    b.Property<DateTime>("Time");

                    b.Property<double?>("YawShare");

                    b.HasKey("DialogueFrameId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueFrames");
                });

            modelBuilder.Entity("HBData.Models.DialogueHint", b =>
                {
                    b.Property<int>("DialogueHintId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid?>("DialogueId");

                    b.Property<string>("HintText");

                    b.Property<bool>("IsAutomatic");

                    b.Property<bool>("IsPositive");

                    b.Property<string>("Type");

                    b.HasKey("DialogueHintId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueHints");
                });

            modelBuilder.Entity("HBData.Models.DialogueInterval", b =>
                {
                    b.Property<Guid>("DialogueIntervalId")
                        .ValueGeneratedOnAdd();

                    b.Property<double?>("AngerTone");

                    b.Property<DateTime>("BegTime");

                    b.Property<Guid?>("DialogueId");

                    b.Property<DateTime>("EndTime");

                    b.Property<double?>("FearTone");

                    b.Property<double?>("HappinessTone");

                    b.Property<bool>("IsClient");

                    b.Property<double?>("NeutralityTone");

                    b.Property<double?>("SadnessTone");

                    b.HasKey("DialogueIntervalId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueIntervals");
                });

            modelBuilder.Entity("HBData.Models.DialoguePhrase", b =>
                {
                    b.Property<int>("DialoguePhraseId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid?>("DialogueId");

                    b.Property<bool>("IsClient");

                    b.Property<int?>("PhraseId");

                    b.HasKey("DialoguePhraseId");

                    b.HasIndex("DialogueId");

                    b.HasIndex("PhraseId");

                    b.ToTable("DialoguePhrases");
                });

            modelBuilder.Entity("HBData.Models.DialoguePhraseCount", b =>
                {
                    b.Property<int>("DialoguePhraseCountId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid?>("DialogueId");

                    b.Property<bool>("IsClient");

                    b.Property<int?>("PhraseCount");

                    b.Property<int?>("PhraseTypeId");

                    b.HasKey("DialoguePhraseCountId");

                    b.HasIndex("DialogueId");

                    b.HasIndex("PhraseTypeId");

                    b.ToTable("DialoguePhraseCounts");
                });

            modelBuilder.Entity("HBData.Models.DialoguePhrasePlace", b =>
                {
                    b.Property<int>("DialoguePhrasePlaceId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid?>("DialogueId");

                    b.Property<int?>("PhraseId");

                    b.Property<bool>("Synonyn");

                    b.Property<string>("SynonynText");

                    b.Property<int?>("WordPosition");

                    b.HasKey("DialoguePhrasePlaceId");

                    b.HasIndex("DialogueId");

                    b.HasIndex("PhraseId");

                    b.ToTable("DialoguePhrasePlaces");
                });

            modelBuilder.Entity("HBData.Models.DialogueSpeech", b =>
                {
                    b.Property<Guid>("DialogueSpeechId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid?>("DialogueId");

                    b.Property<bool>("IsClient");

                    b.Property<double?>("PositiveShare");

                    b.Property<double?>("SilenceShare");

                    b.Property<double?>("SpeechSpeed");

                    b.HasKey("DialogueSpeechId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueSpeeches");
                });

            modelBuilder.Entity("HBData.Models.DialogueVisual", b =>
                {
                    b.Property<Guid>("DialogueVisualId")
                        .ValueGeneratedOnAdd();

                    b.Property<double?>("AngerShare");

                    b.Property<double?>("AttentionShare");

                    b.Property<double?>("ContemptShare");

                    b.Property<Guid?>("DialogueId");

                    b.Property<double?>("DisgustShare");

                    b.Property<double?>("FearShare");

                    b.Property<double?>("HappinessShare");

                    b.Property<double?>("NeutralShare");

                    b.Property<double?>("SadnessShare");

                    b.Property<double?>("SurpriseShare");

                    b.HasKey("DialogueVisualId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueVisuals");
                });

            modelBuilder.Entity("HBData.Models.DialogueWord", b =>
                {
                    b.Property<Guid>("DialogueWordId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("BegTime");

                    b.Property<Guid?>("DialogueId");

                    b.Property<DateTime>("EndTime");

                    b.Property<bool>("IsClient");

                    b.Property<string>("Word");

                    b.HasKey("DialogueWordId");

                    b.HasIndex("DialogueId");

                    b.ToTable("DialogueWords");
                });

            modelBuilder.Entity("HBData.Models.Language", b =>
                {
                    b.Property<int>("LanguageId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("LanguageLocalName");

                    b.Property<string>("LanguageName");

                    b.Property<string>("LanguageShortName");

                    b.HasKey("LanguageId");

                    b.ToTable("Languages");
                });

            modelBuilder.Entity("HBData.Models.Payment", b =>
                {
                    b.Property<int>("PaymentId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("CompanyId");

                    b.Property<DateTime>("Date");

                    b.Property<double>("PaymentAmount");

                    b.Property<string>("PaymentComment");

                    b.Property<double>("PaymentTime");

                    b.Property<int?>("StatusId");

                    b.Property<string>("TransactionId");

                    b.HasKey("PaymentId");

                    b.HasIndex("CompanyId");

                    b.HasIndex("StatusId");

                    b.ToTable("Payments");
                });

            modelBuilder.Entity("HBData.Models.Phrase", b =>
                {
                    b.Property<int>("PhraseId")
                        .ValueGeneratedOnAdd();

                    b.Property<double?>("Accurancy");

                    b.Property<bool>("IsClient");

                    b.Property<int?>("LanguageId");

                    b.Property<string>("PhraseText");

                    b.Property<int?>("PhraseTypeId");

                    b.Property<bool>("Template");

                    b.Property<int?>("WordsSpace");

                    b.HasKey("PhraseId");

                    b.HasIndex("LanguageId");

                    b.HasIndex("PhraseTypeId");

                    b.ToTable("Phrases");
                });

            modelBuilder.Entity("HBData.Models.PhraseCompany", b =>
                {
                    b.Property<int>("PhraseCompanyId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("CompanyId");

                    b.Property<int?>("PhraseId");

                    b.HasKey("PhraseCompanyId");

                    b.HasIndex("CompanyId");

                    b.HasIndex("PhraseId");

                    b.ToTable("PhraseCompanies");
                });

            modelBuilder.Entity("HBData.Models.PhraseType", b =>
                {
                    b.Property<int>("PhraseTypeId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Colour");

                    b.Property<string>("ColourSyn");

                    b.Property<string>("PhraseTypeText");

                    b.HasKey("PhraseTypeId");

                    b.ToTable("PhraseTypes");
                });

            modelBuilder.Entity("HBData.Models.Session", b =>
                {
                    b.Property<int>("SessionId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ApplicationUserId");

                    b.Property<DateTime>("BegTime");

                    b.Property<DateTime>("EndTime");

                    b.Property<bool>("IsDesktop");

                    b.Property<int?>("StatusId");

                    b.HasKey("SessionId");

                    b.HasIndex("ApplicationUserId");

                    b.HasIndex("StatusId");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("HBData.Models.Status", b =>
                {
                    b.Property<int>("StatusId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("StatusName")
                        .IsRequired();

                    b.HasKey("StatusId");

                    b.ToTable("Statuses");
                });

            modelBuilder.Entity("HBData.Models.Tariff", b =>
                {
                    b.Property<Guid>("TariffId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("CompanyId");

                    b.Property<DateTime>("CreationDate");

                    b.Property<string>("CustomerKey");

                    b.Property<int>("EmployeeNo");

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<string>("Rebillid");

                    b.Property<int?>("StatusId");

                    b.Property<string>("TariffComment");

                    b.Property<byte[]>("Token");

                    b.Property<decimal>("TotalRate");

                    b.Property<bool>("isMonthly");

                    b.HasKey("TariffId");

                    b.HasIndex("CompanyId");

                    b.HasIndex("StatusId");

                    b.ToTable("Tariffs");
                });

            modelBuilder.Entity("HBData.Models.Transaction", b =>
                {
                    b.Property<Guid>("TransactionId")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("Amount");

                    b.Property<string>("OrderId");

                    b.Property<DateTime>("PaymentDate");

                    b.Property<string>("PaymentId");

                    b.Property<int?>("StatusId");

                    b.Property<Guid?>("TariffId");

                    b.Property<string>("TransactionComment");

                    b.HasKey("TransactionId");

                    b.HasIndex("StatusId");

                    b.HasIndex("TariffId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("HBData.Models.WorkerType", b =>
                {
                    b.Property<int>("WorkerTypeId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CompanyId");

                    b.Property<string>("WorkerTypeName");

                    b.HasKey("WorkerTypeId");

                    b.HasIndex("CompanyId");

                    b.ToTable("WorkerTypes");
                });

            modelBuilder.Entity("HBLib.Models.Dialogue", b =>
                {
                    b.Property<Guid>("DialogueId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ApplicationUserId");

                    b.Property<DateTime>("BegTime");

                    b.Property<string>("Comment");

                    b.Property<DateTime>("CreationTime");

                    b.Property<DateTime>("EndTime");

                    b.Property<bool>("InStatistic");

                    b.Property<int?>("LanguageId");

                    b.Property<int?>("StatusId");

                    b.Property<string>("SysVersion");

                    b.HasKey("DialogueId");

                    b.HasIndex("ApplicationUserId");

                    b.HasIndex("LanguageId");

                    b.HasIndex("StatusId");

                    b.ToTable("Dialogues");
                });

            modelBuilder.Entity("HBData.Models.ApplicationUser", b =>
                {
                    b.HasOne("HBData.Models.Company", "Company")
                        .WithMany("ApplicationUser")
                        .HasForeignKey("CompanyId");

                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany("ApplicationUser")
                        .HasForeignKey("StatusId");

                    b.HasOne("HBData.Models.WorkerType", "WorkerType")
                        .WithMany()
                        .HasForeignKey("WorkerTypeId");
                });

            modelBuilder.Entity("HBData.Models.Campaign", b =>
                {
                    b.HasOne("HBData.Models.Company", "Company")
                        .WithMany()
                        .HasForeignKey("CompanyId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany()
                        .HasForeignKey("StatusId");
                });

            modelBuilder.Entity("HBData.Models.CampaignContent", b =>
                {
                    b.HasOne("HBData.Models.Campaign", "Campaign")
                        .WithMany()
                        .HasForeignKey("CampaignId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("HBData.Models.Content", "Content")
                        .WithMany()
                        .HasForeignKey("ContentId");
                });

            modelBuilder.Entity("HBData.Models.CampaignContentSession", b =>
                {
                    b.HasOne("HBData.Models.ApplicationUser", "ApplicationUser")
                        .WithMany()
                        .HasForeignKey("ApplicationUserId");

                    b.HasOne("HBData.Models.CampaignContent", "CampaignContent")
                        .WithMany()
                        .HasForeignKey("CampaignContentId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("HBData.Models.Company", b =>
                {
                    b.HasOne("HBData.Models.CompanyIndustry", "CompanyIndustry")
                        .WithMany("Company")
                        .HasForeignKey("CompanyIndustryId");

                    b.HasOne("HBData.Models.Corporation", "Corporation")
                        .WithMany()
                        .HasForeignKey("CorporationId");

                    b.HasOne("HBData.Models.Country", "Country")
                        .WithMany("Company")
                        .HasForeignKey("CountryId");

                    b.HasOne("HBData.Models.Language", "Language")
                        .WithMany("Company")
                        .HasForeignKey("LanguageId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany()
                        .HasForeignKey("StatusId");
                });

            modelBuilder.Entity("HBData.Models.Content", b =>
                {
                    b.HasOne("HBData.Models.Company", "Company")
                        .WithMany()
                        .HasForeignKey("CompanyId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("HBData.Models.DialogueAudio", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueAudio")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialogueClientProfile", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueClientProfile")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialogueClientSatisfaction", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueClientSatisfaction")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialogueFrame", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueFrame")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialogueHint", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany()
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialogueInterval", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueInterval")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialoguePhrase", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany()
                        .HasForeignKey("DialogueId");

                    b.HasOne("HBData.Models.Phrase", "Phrase")
                        .WithMany()
                        .HasForeignKey("PhraseId");
                });

            modelBuilder.Entity("HBData.Models.DialoguePhraseCount", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialoguePhraseCount")
                        .HasForeignKey("DialogueId");

                    b.HasOne("HBData.Models.PhraseType", "PhrType")
                        .WithMany()
                        .HasForeignKey("PhraseTypeId");
                });

            modelBuilder.Entity("HBData.Models.DialoguePhrasePlace", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialoguePhrasePlace")
                        .HasForeignKey("DialogueId");

                    b.HasOne("HBData.Models.Phrase", "Phrase")
                        .WithMany()
                        .HasForeignKey("PhraseId");
                });

            modelBuilder.Entity("HBData.Models.DialogueSpeech", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueSpeech")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialogueVisual", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueVisual")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.DialogueWord", b =>
                {
                    b.HasOne("HBLib.Models.Dialogue", "Dialogue")
                        .WithMany("DialogueWord")
                        .HasForeignKey("DialogueId");
                });

            modelBuilder.Entity("HBData.Models.Payment", b =>
                {
                    b.HasOne("HBData.Models.Company", "Company")
                        .WithMany("Payment")
                        .HasForeignKey("CompanyId");

                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany()
                        .HasForeignKey("StatusId");
                });

            modelBuilder.Entity("HBData.Models.Phrase", b =>
                {
                    b.HasOne("HBData.Models.Language", "Language")
                        .WithMany()
                        .HasForeignKey("LanguageId");

                    b.HasOne("HBData.Models.PhraseType", "PhraseType")
                        .WithMany("Phrase")
                        .HasForeignKey("PhraseTypeId");
                });

            modelBuilder.Entity("HBData.Models.PhraseCompany", b =>
                {
                    b.HasOne("HBData.Models.Company", "Company")
                        .WithMany()
                        .HasForeignKey("CompanyId");

                    b.HasOne("HBData.Models.Phrase", "Phrase")
                        .WithMany("PhraseCompany")
                        .HasForeignKey("PhraseId");
                });

            modelBuilder.Entity("HBData.Models.Session", b =>
                {
                    b.HasOne("HBData.Models.ApplicationUser", "ApplicationUser")
                        .WithMany("Session")
                        .HasForeignKey("ApplicationUserId");

                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany()
                        .HasForeignKey("StatusId");
                });

            modelBuilder.Entity("HBData.Models.Tariff", b =>
                {
                    b.HasOne("HBData.Models.Company", "Company")
                        .WithMany()
                        .HasForeignKey("CompanyId");

                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany()
                        .HasForeignKey("StatusId");
                });

            modelBuilder.Entity("HBData.Models.Transaction", b =>
                {
                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany()
                        .HasForeignKey("StatusId");

                    b.HasOne("HBData.Models.Tariff", "Tariff")
                        .WithMany()
                        .HasForeignKey("TariffId");
                });

            modelBuilder.Entity("HBData.Models.WorkerType", b =>
                {
                    b.HasOne("HBData.Models.Company", "Company")
                        .WithMany()
                        .HasForeignKey("CompanyId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("HBLib.Models.Dialogue", b =>
                {
                    b.HasOne("HBData.Models.ApplicationUser", "ApplicationUser")
                        .WithMany("Dialogue")
                        .HasForeignKey("ApplicationUserId");

                    b.HasOne("HBData.Models.Language", "Language")
                        .WithMany("Dialogue")
                        .HasForeignKey("LanguageId");

                    b.HasOne("HBData.Models.Status", "Status")
                        .WithMany("Dialogue")
                        .HasForeignKey("StatusId");
                });
#pragma warning restore 612, 618
        }
    }
}
