using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using HBData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using HBLib;
using HBLib.Utils;
using System.Threading;
using System.Diagnostics;
using System.Net.Mime;
using RazorLight;
using RazorLight.Razor;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using UserOperations.Services;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using System.Security.Cryptography;
using System.Security.Claims;
using HBData.Models;
using UserOperations.Models.AnalyticModels;
using ZedGraph;
using System.Drawing;
using QuartzExtensions.Utils.WeeklyReport;

namespace QuartzExtensions.Jobs
{
    public class SendUserAnalyticReportJob : IJob
    {
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private ElasticClient _log;
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;
        private readonly AccountAuthorization _autorizationData;
        private WeeklyReport _weeklyReport;

        public SendUserAnalyticReportJob(
            ILoginService loginService,
            IServiceScopeFactory factory, 
            ElasticClientFactory elasticClientFactory,
            SmtpSettings smtpSettings,
            SmtpClient smtpClient,
            AccountAuthorization autorizationData,
            WeeklyReport weeklyReport)
        {                  
            _loginService = loginService;    
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();    
            _elasticClientFactory = elasticClientFactory;   
            _smtpSettings = smtpSettings;  
            _smtpClient = smtpClient;
            _log = _elasticClientFactory.GetElasticClient();  
            _autorizationData = autorizationData;
            _weeklyReport = weeklyReport;
        }

        public async Task Execute(IJobExecutionContext context)
        {   
            System.Console.WriteLine($"executed");
            if(!System.IO.Directory.Exists("static/html/"))
                System.IO.Directory.CreateDirectory("static/html/");  

            var applicationUsers = _context.ApplicationUsers
                .Include(p => p.Company)
                .Where(p => p.StatusId == 3)
                .ToList();
            
            var sesBegTime = DateTime.Now.AddDays(-7);
            var sesEndTime = DateTime.Now.AddDays(-1);
            var sessions = _context.Sessions.Where(p => p.BegTime.Date >=sesBegTime
                    && p.EndTime.Date <=sesEndTime
                    && p.StatusId == 7)
                .ToList();            
            var counter =0;
            foreach(var user in applicationUsers)
            {
                var userSessions = sessions.Where(p => p.ApplicationUserId == user.Id).ToList();
                if(userSessions == null || userSessions.Count == 0)
                {
                    _log.Error($"Sessions for user: {user.Id} is null");                
                }
                else
                {
                    if(!user.Email.Contains("@heedbook.com"))
                    {
                        _log.Info($"Prepare report for applicationUser {user.Id} - {user.FullName}");        
                        //await CreateHtmlWeeklyReport(user);
                        await _weeklyReport.CreateHtmlWeeklyReport(user);
                        await _weeklyReport.SendHttpReport();
                        counter++;                        
                    }
                }
            } 
            _log.Info($"Weekly report sent to {counter} users"); 
            try
            {
                var mail = new System.Net.Mail.MailMessage();
                mail.From = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail);
                
                mail.To.Add("krokhmal11@mail.ru");
                mail.To.Add("pinarin@heedbook.com");
                mail.Subject = "Users Weekly Analytic Reports Status";
                mail.Body = $"Weekly report sent to {counter} users";
                mail.IsBodyHtml = false;                        
                _smtpClient.Send(mail); 
                System.Console.WriteLine($"Weekly report sent to {counter} users");
            }   
            catch(Exception ex)
            {
                _log.Fatal($"Failed send email to krokhmal and pinarin \n{ex.Message}");
            }   
        }           
    }      
}
