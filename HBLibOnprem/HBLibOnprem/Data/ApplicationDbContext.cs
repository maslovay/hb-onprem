using HBLib.Models;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;

namespace HBLib.Data {
    public class RecordsContext  : DbContext
    {
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

            modelBuilder.Entity<AspNetUserRole>()
                .HasKey(r => r.UserId);
            modelBuilder.Entity<AspNetRole>()
                .HasKey(r => r.Id);
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<AspNetRole> AspNetRoles { get; set; }
        public DbSet<AspNetUserRole> AspNetUserRoles { get; set; }
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
        public DbSet<DialoguePhrase> DialoguePhrases { get; set; }
        public DbSet<DialoguePhraseCount> DialoguePhraseCounts { get; set; }
        public DbSet<DialoguePhrasePlace> DialoguePhrasePlaces { get; set; }
        public DbSet<DialogueSpeech> DialogueSpeechs { get; set; }
        public DbSet<DialogueTargetMediaContent> DialogueTargetMediaContents { get; set; }
        public DbSet<DialogueVisual> DialogueVisuals { get; set; }
        public DbSet<DialogueWord> DialogueWords { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<MediaContent> MediaContents { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Phrase> Phrases { get; set; }
        public DbSet<PhraseCompany> PhraseCompanys { get; set; }
        public DbSet<PhraseType> PhraseTypes { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Status> Statuss { get; set; }
        public DbSet<TargetGroup> TargetGroups { get; set; }
        public DbSet<TargetMediaContent> TargetMediaContents { get; set; }
        public DbSet<TargetMediaContentInterface> TargetMediaContentInterfaces { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<WorkerType> WorkerTypes { get; set; }
    }

}
