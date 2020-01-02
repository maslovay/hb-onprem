﻿using System;
using HBData.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HBData
{
    public class RecordsContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, IdentityUserClaim<Guid>,
        ApplicationUserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        public RecordsContext() : base()
        {
        }

        public RecordsContext(DbContextOptions<RecordsContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseNpgsql("User ID=test_user;Password=test_password;Host=104.40.181.96;Port=5432;Database=test_db;Pooling=true;Timeout=120;CommandTimeout=0");
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(i =>
            {
                i.ToTable("AspNetUsers");
                i.HasKey(x => x.Id);
                i.HasMany(x => x.UserRoles)
                 .WithOne()
                 .HasForeignKey(p => p.UserId).IsRequired();
            });
            builder.Entity<ApplicationRole>(i =>
            {
                i.ToTable("AspNetRole");
                i.HasKey(x => x.Id);
                i.HasMany(x => x.UserRoles).WithOne().HasForeignKey(p => p.RoleId).IsRequired();
            });

            builder.Entity<IdentityUserLogin<Guid>>(i =>
            {
                i.ToTable("AspNetUserLogins");
                i.HasKey(x => new {x.ProviderKey, x.LoginProvider});
            });
            builder.Entity<IdentityRoleClaim<Guid>>(i =>
            {
                i.ToTable("AspNetRoleClaims");
                i.HasKey(x => x.Id);
            });
            builder.Entity<IdentityUserClaim<Guid>>(i =>
            {
                i.ToTable("AspNetUserClaims");
                i.HasKey(x => x.Id);
            });
            builder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.HasKey(ur => new {ur.UserId, ur.RoleId});

                userRole.HasOne(ur => ur.Role)
                        .WithMany(r => r.UserRoles)
                        .HasForeignKey(ur => ur.RoleId)
                        .IsRequired();

                userRole.HasOne(ur => ur.User)
                        .WithMany(r => r.UserRoles)
                        .HasForeignKey(ur => ur.UserId)
                        .IsRequired()
                    //.HasPrincipalKey(p => p.Id)
                    ;
                userRole.ToTable("AspNetUserRoles");
            });
        }
        
        
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }
        public DbSet<ApplicationUserRole> ApplicationUserRoles { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignContent> CampaignContents { get; set; }
        public DbSet<CampaignContentAnswer> CampaignContentAnswers {get;set;}
        public DbSet<SlideShowSession> SlideShowSessions { get; set; }
        public DbSet<CatalogueHint> CatalogueHints { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<Company> Companys { get; set; }
        public DbSet<Corporation> Corporations { get; set; }
        public DbSet<CompanyIndustry> CompanyIndustrys { get; set; }
        public DbSet<Country> Countrys { get; set; }
        public DbSet<Dialogue> Dialogues { get; set; }
        public DbSet<DialogueAudio> DialogueAudios { get; set; }
        public DbSet<DialogueClientProfile> DialogueClientProfiles { get; set; }
        public DbSet<DialogueHint> DialogueHints { get; set; }
        public DbSet<DialogueClientSatisfaction> DialogueClientSatisfactions { get; set; }
        public DbSet<DialogueFrame> DialogueFrames { get; set; }
        public DbSet<DialogueInterval> DialogueIntervals { get; set; }
        public DbSet<DialogueMarkup> DialogueMarkups { get; set; }
        public DbSet<DialoguePhrase> DialoguePhrases { get; set; }
        public DbSet<DialoguePhraseCount> DialoguePhraseCounts { get; set; }
        public DbSet<DialogueSpeech> DialogueSpeechs { get; set; }
        public DbSet<DialogueVisual> DialogueVisuals { get; set; }
        public DbSet<DialogueWord> DialogueWords { get; set; }
        public DbSet<FileAudioDialogue> FileAudioDialogues { get; set; }
        public DbSet<FileAudioEmployee> FileAudioEmployees { get; set; }
        public DbSet<FileFrame> FileFrames { get; set; }
        public DbSet<FileVideo> FileVideos { get; set; }
        public DbSet<FrameAttribute> FrameAttributes { get; set; }
        public DbSet<FrameEmotion> FrameEmotions { get; set; }
        public DbSet<GoogleAccount> GoogleAccounts { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Phrase> Phrases { get; set; }
        public DbSet<PhraseCompany> PhraseCompanys { get; set; }
        public DbSet<PhraseType> PhraseTypes { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Status> Statuss { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<AlertType> AlertTypes { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<TabletAppInfo> TabletAppInfos { get; set; }
        public DbSet<VideoFace> VideoFaces { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ClientNote> ClientNotes { get; set; }

        public DbSet<Benchmark> Benchmarks { get; set; }
        public DbSet<BenchmarkName> BenchmarkNames { get; set; }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceType> DeviceTypes { get; set; }
        //---VIES
        public DbSet<VWeeklyUserReport> VWeeklyUserReports { get; set; }
        public DbSet<VSessionUserWeeklyReport> VSessionUserWeeklyReports { get; set; }
    }
}