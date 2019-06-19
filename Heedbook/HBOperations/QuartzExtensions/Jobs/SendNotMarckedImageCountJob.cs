using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using HBData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace QuartzExtensions.Jobs
{
    public class SendNotMarckedImageCountJob : IJob
    {
        private readonly RecordsContext _context;

        public SendNotMarckedImageCountJob(IServiceScopeFactory factory)
        {
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            Console.WriteLine("Конструктор SendNotMarckedImageCountJob");
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Отправка письма!");
            var mail = new MailMessage();
            mail.From = new MailAddress("kertak@yandex.ru");
            mail.To.Add(new MailAddress("pinarin@heedbook.com"));
            mail.Subject = "Letter Topic";
            var data = NotMarckedImageCount();
            var mailData = "";
            foreach (var item in data) mailData += item.CompanyName + " " + item.Count + "\n";

            mail.Body = mailData;
            mail.IsBodyHtml = false;
            //mail.Attachments.Add(new Attachment("/home/oleg/Документы/My_Saves/save1.txt"));

            //For Yandex
            var host = "smtp.yandex.ru";
            var port = 25;

            var smtpClient = new SmtpClient(host, port);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;

            var password = "querty";

            smtpClient.Credentials = new NetworkCredential("kertak@yandex.ru", password);
            smtpClient.Send(mail);            
        }

        public List<CompanyFrameInformation> NotMarckedImageCount()
        {
            var frames = _context.FileFrames
                                 .Include(p => p.ApplicationUser)
                                 .Include(p => p.ApplicationUser.Company)
                                 .Where(p => p.StatusId == 5) //                
                                 .GroupBy(p => p.ApplicationUser.CompanyId)
                                 .Select(p => new CompanyFrameInformation
                                  {
                                      CompanyId = p.Key,
                                      CompanyName = p.First().ApplicationUser.Company.CompanyName,
                                      Count = p.Count()
                                  })
                                 .ToList();
            return frames;
        }
    }

    public class CompanyFrameInformation
    {
        public Guid? CompanyId { get; set; }
        public String CompanyName { get; set; }
        public Int32 Count { get; set; }
    }
}