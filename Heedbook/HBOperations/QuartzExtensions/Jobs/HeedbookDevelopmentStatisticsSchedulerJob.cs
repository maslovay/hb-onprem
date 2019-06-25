using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
    
        public HeedbookDevelopmentStatisticsJob(IServiceScopeFactory factory, ElasticClientFactory elasticClientFactory)
        {            
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();         
            var mail = new MailMessage();
            mail.From = new MailAddress("support@heedbook.com");            
            mail.To.Add(new MailAddress("support@heedbook.com"));
            
            mail.Subject = "Heedbook development statistics";
            var data = HeedbookDevelopmentStatistics();                 

            mail.Body = data;
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.IsBodyHtml = false;
            
            var host = "smtp.yandex.ru";
            var port = 587;

            var smtpClient = new SmtpClient(host, port);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Timeout = 10000;

            var password = "Heedbook_2017";

            smtpClient.Credentials = new NetworkCredential("support@heedbook.com", password);            
            try
            {
                smtpClient.Send(mail);
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
            var result = "NEW COMPANIES IN THE LAST 48 HOURS:\n\n";
            var newCompanyes = _context.Companys
                                        .Where(p => p.CreationDate > DateTime.UtcNow.AddHours(-48))
                                        .ToList();
            foreach(var c in newCompanyes)
            {
                result += $"{c.CreationDate} - {c.CompanyName}\n";   
            }            
            
            result += $"\n\nACTIVITY OF COMPANIES FOR LAST DAYS:\n\n";            

            var dialogues = _context.Dialogues
                                    .Include(p => p.ApplicationUser)
                                    .Where(p => p.BegTime > DateTime.UtcNow.AddHours(-24));                                    

            var videos = _context.FileVideos
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime > DateTime.UtcNow.AddHours(-24)).ToList();
            
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
                        TotalSessionDuration = GetTotalSessionDuration(p).ToString("d'd 'h'h 'm'm 's's'"),
                        TotalVideoDuration = GetTotalVideoDuration(p.First().ApplicationUser.CompanyId, ref videos)
                            .ToString("d'd 'h'h 'm'm 's's'"),
                        CountOfDialogues = dialogues.Where(s => s.StatusId == 3
                            && s.ApplicationUser.CompanyId == p.First().ApplicationUser.CompanyId).Count()
                    })
                .ToList();

            foreach(var compRep in sessions)
            {                         
                result += $"Company name:                       {compRep.CompanyName}\n";
                result += $"Number of employes worked:          {compRep.CountOfEmployers}\n";
                result += $"Total session duration:             {compRep.TotalSessionDuration:0.##}\n";
                result += $"Total duration of all videos:       {compRep.TotalVideoDuration:0.##}\n";
                result += $"Number of dialogs with status 3:    {compRep.CountOfDialogues}\n\n";
            }
            return result;
        }

        public TimeSpan GetTotalSessionDuration(IGrouping<Guid?, Session> Sessions)
        {            
            TimeSpan result = TimeSpan.Zero;            
            foreach(var session in Sessions)
            {
                result += session.EndTime.Subtract(session.BegTime);
            }         
            return result;
        }
        public TimeSpan GetTotalVideoDuration(Guid? companyId, ref List<FileVideo> videos)
        {            
            var companyVideos = videos.Where(p => p.ApplicationUser.CompanyId == companyId).ToList();
            TimeSpan result = TimeSpan.Zero;
            foreach(var video in companyVideos)
            {
                result += TimeSpan.FromSeconds(video.Duration == null ? 0 : (double)video.Duration);
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
    }
}