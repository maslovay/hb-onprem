using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using HBData;
using HBData.Models;
using HBLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Quartz;

namespace CountingPetitionScheduler.QuartzJob
{
    public class CountingPetitionJob : IJob
    {
        private ElasticClientFactory _elasticClientFactory;
        private readonly RecordsContext _context;

        public CountingPetitionJob(RecordsContext context,
            ElasticClientFactory elasticClientFactory)
        {
            _context = context;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dictPositiveHints = _context.Companys
                .Where(i => i.ApplicationUser
                    .Any(a => a.Dialogue
                        .Any(s => s.StatusId == 3 &&
                                  s.CreationTime >=
                                  DateTime.UtcNow.AddDays(-30) &&
                                  s.DialogueHint.Any())))
                .Include(x => x.ApplicationUser)
                .ThenInclude(q => q.Dialogue)
                .ThenInclude(d => d.DialogueHint)
                .ToDictionary(q => q.CompanyName, q => new
                {
                    Postitives = q.ApplicationUser
                        .SelectMany(a => a.Dialogue)
                        .Where(d => d.StatusId == 3 &&
                                    d.CreationTime >=
                                    DateTime.UtcNow.AddDays(-30))
                        .SelectMany(d => d.DialogueHint)
                        .Where(dh => dh.IsPositive)
                        .GroupBy(dh => dh.HintText)
                        .Select(dh => new
                        {
                            Text = dh.Key,
                            Count = dh.Select(e => e.HintText).Count()
                        })
                        .OrderByDescending(b => b.Count)
                        .Select(dh => dh.Text)
                        .Take(3).ToList(),
                    Negatives = q.ApplicationUser
                        .SelectMany(a => a.Dialogue)
                        .Where(d => d.StatusId == 3 &&
                                    d.CreationTime >=
                                    DateTime.UtcNow.AddDays(-30))
                        .SelectMany(d => d.DialogueHint)
                        .Where(dh => !dh.IsPositive)
                        .GroupBy(dh => dh.HintText)
                        .Select(dh => new
                        {
                            Text = dh.Key,
                            Count = dh.Select(e => e.HintText).Count()
                        })
                        .OrderByDescending(b => b.Count)
                        .Select(dh => dh.Text)
                        .Take(3).ToList()
                });
            foreach (var (companyName, pan) in dictPositiveHints)
            {
                SendMessage(companyName, pan.Postitives, pan.Negatives);
            }
        }

        private void SendMessage(string companyName, List<string> positives, List<string> negatives)
        {
            MailAddress from = new MailAddress("dddeee123@yandex.ru", "Dmitriy");
            MailAddress to = new MailAddress("tardiprog@heedbook.com");
            MailMessage message = new MailMessage(from, to);
            message.Subject = "Отчёт";
            message.Body = $"<h2>Company = {companyName} </h2>" +
                           $"<h2>Positive hints = {String.Join(',', positives)}</h2>" +
                           $"<h2>Negative hints = {String.Join(',', negatives)}</h2>";
            message.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient("smtp.yandex.ru", 587);
            smtp.Credentials = new NetworkCredential("dddeee123@yandex.ru", "55427652x");
            smtp.EnableSsl = true;
            smtp.Send(message);
        }
    }
}