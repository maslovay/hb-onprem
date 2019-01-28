using System;
using HBData.Models;
using Microsoft.EntityFrameworkCore;

namespace HBData {

    public class RecordsContext: DbContext
    {
        public RecordsContext(DbContextOptions options): base(options)
        {
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

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
        public DbSet<DialogueMarkup> DialogueMarkups { get; set; }
        public DbSet<DialoguePhraseCount> DialoguePhraseCounts { get; set; }
        public DbSet<DialogueSpeech> DialogueSpeechs { get; set; }
        public DbSet<DialogueVisual> DialogueVisuals { get; set; }
        public DbSet<DialogueWord> DialogueWords { get; set; }
        public DbSet<FileAudioDialogue> FileAudioDialogues{ get; set; }
        public DbSet<FileAudioEmployee> FileAudioEmployees { get; set; }
        public DbSet<FileFrame> FileFrames { get; set; }
        public DbSet<FileVideo> FileVideos { get; set; }
        public DbSet<FrameAttribute> FrameAttributes { get; set; }
        public DbSet<FrameEmotion> FrameEmotions { get; set; }
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
