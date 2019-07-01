using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using HBLib;
using HBLib.Utils;

namespace QuartzExtensions.Jobs
{    
    public class HeedbookDevelopmentStatisticsJob : IJob
    {
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly ElasticClient _log;
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;
    
        public HeedbookDevelopmentStatisticsJob(IServiceScopeFactory factory, 
            ElasticClientFactory elasticClientFactory,
            SmtpSettings smtpSettings,
            SmtpClient smtpClient)
        {            
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _smtpSettings = smtpSettings;  
            _smtpClient = smtpClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();         
            var mail = new System.Net.Mail.MailMessage();
            mail.From = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail);            
            mail.To.Add(new System.Net.Mail.MailAddress("krokhmal11@mail.ru"));
            mail.To.Add(new System.Net.Mail.MailAddress(_smtpSettings.ToEmail)); 
            
            mail.Subject = "Heedbook development statistics";
            
            var data = HeedbookDevelopmentStatistics();                 

            mail.Body = data;
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.IsBodyHtml = false;
                      
            try
            {
                _smtpClient.Send(mail);
                _log.Info("Mail Sended to support@heedbook.com");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                _log.Fatal($"Failed send email to support@heedbook.com\n{ex.Message}\n");
            }
        }

        public string HeedbookDevelopmentStatistics()
        {       
            var result = "NEW COMPANIES FOR LAST 48 HOURS:\n\n";
            var newCompanyes = _context.Companys
                                        .Where(p => p.CreationDate > DateTime.UtcNow.AddHours(-48))
                                        .ToList();
            foreach(var c in newCompanyes)
            {
                result += $"{c.CreationDate} - {c.CompanyName}\n";   
            }            
            
            result += $"\n\nACTIVITY OF COMPANIES FOR LAST 24 HOURS:\n\n";            

            var dialogues = _context.Dialogues
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime > DateTime.UtcNow.AddHours(-24)
                    && p.StatusId == 3)
                .ToList();                                    

            var frames = _context.FileFrames
                .Include(p => p.ApplicationUser)
                .Where(p => p.Time > DateTime.UtcNow.AddHours(-24)
                    && p.FaceId != null)
                .ToList();

            var videos = _context.FileVideos
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime > DateTime.UtcNow.AddHours(-24))
                .ToList();
            
            var sessions = _context.Sessions
                .Include(p => p.ApplicationUser)
                .Include(p => p.ApplicationUser.Company)
                .Where(p => p.BegTime > DateTime.UtcNow.AddHours(-24))
                .GroupBy(p => p.ApplicationUser.CompanyId)
                .Select(p => new CompanyReport
                    {
                        CompanyId = p.Key,
                        CompanyName = p.First().ApplicationUser.Company.CompanyName,
                        CountOfEmployers = p.GroupBy(u => u.ApplicationUserId).Count(),
                        TotalSessionDuration = TimeSpan.FromSeconds(p.Sum(s => s.EndTime.Subtract(s.BegTime).TotalSeconds))
                            .ToString("d'd 'h'h 'm'm 's's'"),
                        TotalVideoDuration = TimeSpan.FromSeconds(videos.Where(s => s.ApplicationUser.CompanyId == p.Key)
                            .Sum(o => (double)o.Duration))
                            .ToString("d'd 'h'h 'm'm 's's'"),
                        CountOfDialogues = dialogues.Where(s => s.ApplicationUser.CompanyId == p.First().ApplicationUser.CompanyId).Count(),
                        CountOfFramesWithFaces = frames.Where(s => s.ApplicationUser.CompanyId == p.Key)
                            .Count(),
                        CountOfDifferentFaces = frames.Where(s => s.ApplicationUser.CompanyId == p.Key)
                            .GroupBy(s => s.FaceId).Distinct().Count()
                    })
                .ToList();

            foreach(var compRep in sessions)
            {                         
                result += $"Company name:                       {compRep.CompanyName}\n";
                result += $"Number of employes worked:          {compRep.CountOfEmployers}\n";
                result += $"Total session duration:             {compRep.TotalSessionDuration:0.##}\n";
                result += $"Total duration of all videos:       {compRep.TotalVideoDuration:0.##}\n";
                result += $"Number of dialogs with status 3:    {compRep.CountOfDialogues}\n";
                result += $"Number of frames with faces:        {compRep.CountOfFramesWithFaces}\n";
                result += $"Number of different faces:          {compRep.CountOfDifferentFaces}\n\n";
            }
            return result;
        }
    }

    public class CompanyReport
    {
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int CountOfEmployers { get; set; }
        public string TotalSessionDuration { get; set; }
        public string TotalVideoDuration { get; set; }
        public int CountOfDialogues { get; set; }
        public int CountOfFramesWithFaces { get; set; }
        public int CountOfDifferentFaces {get; set;}
    }
}