using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HBLib.Utils;
using Quartz;
using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using HBLib.Utils;
using Quartz;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace QuartzExtensions.Jobs
{
    public class SendNotMarckedImageCountJob : IJob
    {
        private readonly RecordsContext _context;
        
        public SendNotMarckedImageCountJob ( IServiceScopeFactory factory )
        {
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            Console.WriteLine("Конструктор SendNotMarckedImageCountJob");
        }
        
        public List<CompanyFrameInformation> NotMarckedImageCount()
        {
            var frames = _context.FileFrames
                .Include(p => p.ApplicationUser)
                .Include(p => p.ApplicationUser.Company)
                .Where(p => p.StatusId == 5)//                
                .GroupBy(p => p.ApplicationUser.CompanyId)
                .Select(p=>new CompanyFrameInformation
                {
                    CompanyId = p.Key,
                    CompanyName = p.First().ApplicationUser.Company.CompanyName,
                    Count = p.Count()
                })
                .ToList();
            return frames;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Отправка письма!");
            MailMessage mail = new MailMessage();  
            mail.From = new MailAddress("kertak@yandex.ru");
            mail.To.Add(new MailAddress("pinarin@heedbook.com"));
            mail.Subject = "Letter Topic";
            var data = NotMarckedImageCount();
            String mailData = "";
            foreach (var item in data)
            {
                mailData += (item.CompanyName + " " + item.Count + "\n");
            }
            
            mail.Body = mailData;                
            mail.IsBodyHtml = false;               
            //mail.Attachments.Add(new Attachment("/home/oleg/Документы/My_Saves/save1.txt"));
    
            //For Yandex
            string host = "smtp.yandex.ru";
            int port = 25;
    
            SmtpClient smtpClient = new SmtpClient(host, port){};
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;
            
            var password = "querty";
            
            smtpClient.Credentials = new NetworkCredential("kertak@yandex.ru", password);
            smtpClient.Send(mail);
            Console.ReadLine();
        }
    }
    
    public class CompanyFrameInformation
    {
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int Count { get; set; }
    }
}