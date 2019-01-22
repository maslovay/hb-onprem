using PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace PostgreSQL.Models {

    public class RecordsContext: DbContext
    {
        //public RecordsContext(DbContextOptions<RecordsContext> options) : base(options) {}

        //public RecordsContext(string connectionString = null) : base(GetOptions("User ID=test_user;Password=test_password;Host=13.69.170.129;Port=5432;Database=test_db; Pooling=true; Min Pool Size=0;Max Pool Size=100;Connection Lifetime =0;")) { }

        //public RecordsContext(string connectionString = null) : base(GetOptions("User ID=test_user;Password=test_password;Host=13.69.170.129;Port=5432;Database=test_db; Pooling=true; Min Pool Size=0;Max Pool Size=100;Connection Lifetime =0;")) { }

        private static DbContextOptions GetOptions(string connectionString)
        {
            return SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), connectionString).Options;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("User ID=test_user;Password=test_password;Host=13.69.170.129;Port=5432;Database=test_db; Pooling=true;");
        }

        /* 
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("REPLACEIT");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PhraseCompany>().ToTable("PhraseCompanys");
            modelBuilder.Entity<Company>().ToTable("Companys");
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<Status>().ToTable("Statuss");
            modelBuilder.Entity<DialogueSpeech>().ToTable("DialogueSpeechs");
            modelBuilder.Entity<FileAudioEmployee>().ToTable("FileAudioEmployees");

            modelBuilder.Entity<AspNetUserRole>()
                .HasKey(r => r.UserId);
            modelBuilder.Entity<AspNetRole>()
                .HasKey(r => r.Id);
        }
        */
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<AspNetRole> AspNetRoles { get; set; }
        public DbSet<AspNetUserRole> AspNetUserRoles { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignContent> CampaignContents { get; set; }
        public DbSet<CampaignContentSession> CampaignContentSessions { get; set; }
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
        public DbSet<DialoguePhraseCount> DialoguePhraseCounts { get; set; }
        public DbSet<DialogueSpeech> DialogueSpeechs { get; set; }
        public DbSet<DialogueVisual> DialogueVisuals { get; set; }
        public DbSet<DialogueWord> DialogueWords { get; set; }
        public DbSet<FileAudioEmployee> FileAudioEmployees { get; set; }
        public DbSet<FileFrame> FileFrames { get; set; }
        public DbSet<FileVideo> FileVideos { get; set; } 
        public DbSet<Language> Languages { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Phrase> Phrases { get; set; }
        public DbSet<PhraseCompany> PhraseCompanys { get; set; }
        public DbSet<PhraseType> PhraseTypes { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Status> Statuss { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<WorkerType> WorkerTypes { get; set; }
    }
}
