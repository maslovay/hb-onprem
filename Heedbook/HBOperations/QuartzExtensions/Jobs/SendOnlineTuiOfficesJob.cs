using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using HBLib;
using HBLib.Utils;

namespace QuartzExtensions.Jobs
{
    public class SendOnlineTuiOfficesJob : IJob
    {
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        //private readonly ElasticClient _log;
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;

        private List<Guid?> tuiOfficeGuids = new List<Guid?>
            {
                new Guid("862b66a8-8d09-4363-8504-d5c2dce15e15"),
                new Guid("82560395-2cc3-46e8-bcef-c844f1048182"),
                new Guid("3aacfc92-583f-4f3f-bb3b-e73d933008d7"),
                new Guid("6024892a-f535-46d7-bfc6-d926ee16461c"),
                new Guid("aae8058f-b9a6-4c6a-87f9-3fc78e46eebf"),
                new Guid("b7b60c4c-47bc-45d8-8a8c-11fb12f6047c"),
                new Guid("b7b60c4c-47bc-45d8-8a8c-11fb12f6047c"),
                new Guid("bf51875d-2331-4a57-b30d-a12cff20beab"),
                new Guid("a1e2f8f0-388f-45d7-9dcd-cd5ed8e22436"),
                new Guid("9e55e308-1b96-4d0c-8ca7-cfe7bf26ff82"),
                new Guid("3f1a1253-238c-429c-b319-2c03f95140aa"),
                new Guid("e99c8fe4-3835-4ff4-8f32-7694290492ab"),
                new Guid("690f03f2-6565-413a-a658-227140780c75"),
                new Guid("7b83800e-583a-49a6-9e13-775a095f5baa"),
                new Guid("fbc2d85e-1f32-4521-8ac4-a184ee71b554")
            };
        public SendOnlineTuiOfficesJob(IServiceScopeFactory factory, 
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
            var mail = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail)
            };
            mail.To.Add(new System.Net.Mail.MailAddress(_smtpSettings.ToEmail));
            mail.To.Add($"pinarin@heedbook.com");
            
            mail.Subject = "Active TUI OFFICES";
            var data = TuiOnlineOffices();
            var mailData = "";
            foreach (var item in data) mailData += item.CompanyName + " " + item.ApplicationUserName + " Online" + "\n";

            mail.Body = mailData;
            mail.IsBodyHtml = false;  
            
            try
            {
                _smtpClient.Send(mail);       
                _log.Info("Mail Sent to anisiya.kobylina@tui.ru");              
            }
            catch(Exception ex)
            {
                _log.Fatal($"Failed send email to anisiya.kobylina@tui.ru\n{ex.Message}\n");
            }  
        }


        public List<CompanyInformation> TuiOnlineOffices()
        {           
            var offices = _context.Sessions
            .Include(p=>p.ApplicationUser)
            .Include(p=>p.Device.Company)
            .Where(p=>p.StatusId == 6
                &&tuiOfficeGuids.Contains(p.Device.CompanyId))
            .GroupBy(p => p.Device.CompanyId)
            .Select(p=> new CompanyInformation
                {
                    CompanyId = p.Key,
                    CompanyName = p.First().Device.Company.CompanyName,
                    ApplicationUserName = p.First().ApplicationUser!=null? p.First().ApplicationUser.FullName : null
                })
            .ToList();            
            return offices;            
        }
    }

    public class CompanyInformation
    {
        public Guid? CompanyId { get; set; }
        public String CompanyName { get; set; }    
        public String ApplicationUserName {get; set;}   
    }
}