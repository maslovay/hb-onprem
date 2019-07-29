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

        public SendUserAnalyticReportJob(
            ILoginService loginService,
            IServiceScopeFactory factory, 
            ElasticClientFactory elasticClientFactory,
            SmtpSettings smtpSettings,
            SmtpClient smtpClient,
            AccountAuthorization autorizationData)
        {                  
            _loginService = loginService;    
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();    
            _elasticClientFactory = elasticClientFactory;   
            _smtpSettings = smtpSettings;  
            _smtpClient = smtpClient;
            _log = _elasticClientFactory.GetElasticClient();  
            _autorizationData = autorizationData;
        }

        public async Task Execute(IJobExecutionContext context)
        {   
            if(!System.IO.Directory.Exists("static/html/"))
                System.IO.Directory.CreateDirectory("static/html/");  

            var applicationUsers = _context.ApplicationUsers
                .Include(p => p.Company)
                .Where(p => p.StatusId == 3);
            foreach(var user in applicationUsers)
            {
                await CreateHtmlFromTemplate(user);
            }
        }
        private async Task CreateHtmlFromTemplate(ApplicationUser applicationUser)
        {
            var sesBegTime = DateTime.Now.AddDays(-7);
            var sesEndTime = DateTime.Now.AddDays(-1);
            var sessions = _context.Sessions.Where(p => p.ApplicationUserId == applicationUser.Id
                    && p.BegTime.Date >=sesBegTime
                    && p.EndTime.Date <=sesEndTime
                    && p.StatusId == 7)
                .ToList();
            if(sessions == null || sessions.Count == 0)
            {
                _log.Error($"Sessions for user: {applicationUser.Id} is null");
                return;
            }
                
            var fullPath = System.IO.Path.GetFullPath(".");
            var engine = new RazorLight.RazorLightEngineBuilder()
                .UseFilesystemProject(fullPath)
                .UseMemoryCachingProvider()
                .Build();

            var languageId = applicationUser.Company.LanguageId;
            var languageTableData = File.ReadAllText("language_table.json");

            var languageTable = JsonConvert.DeserializeObject<List<LanguageDataReport>>(languageTableData);            
            var languageDataReport = languageTable[languageId==null ? 0 : (int)languageId-1]; 

            var userData = await GetUserWeeklyData(applicationUser);    

            var  UserWeeklyInfo= JsonConvert.DeserializeObject<WeeklyReport>(userData.Authorization);            
            if(UserWeeklyInfo == null)
            {
                _log.Error($"Weekly Data is empty");
                return;
            }
            List<ReportData> parameters = new List<ReportData>
                {
                    new ReportData{Name = "Satisfaction", Data = UserWeeklyInfo.Satisfaction, Description = languageDataReport.Indicators.Satisfaction, Thumbnail = true, ColourData=UserWeeklyInfo.Satisfaction.totalAvg},
                    new ReportData{Name = "PositiveEmotions", Data = UserWeeklyInfo.PositiveEmotions, Description = languageDataReport.Report.Indicators.PositiveEmotions, ColourData=UserWeeklyInfo.PositiveEmotions.totalAvg},
                    new ReportData{Name = "PositiveIntonations", Data = UserWeeklyInfo.PositiveIntonations, Description = languageDataReport.Report.Indicators.PositiveIntonation, ColourData=UserWeeklyInfo.PositiveIntonations.totalAvg},
                    new ReportData{Name = "SpeechEmotivity", Data = UserWeeklyInfo.SpeechEmotivity, Description = languageDataReport.Report.Indicators.SpeechEmotivity, ColourData=UserWeeklyInfo.SpeechEmotivity.totalAvg},
                    new ReportData{Name = "Workload", Data = UserWeeklyInfo.Workload, Description = languageDataReport.Indicators.Workload, Thumbnail = true, ColourData=UserWeeklyInfo.Workload.totalAvg},
                    new ReportData{Name = "NumberOfDialogues", Data = UserWeeklyInfo.NumberOfDialogues, Description = languageDataReport.Indicators.NumberOfDialogues, ColourData=UserWeeklyInfo.NumberOfDialogues.totalAvg, NotPercentage = true, Integer=true, ReportStyle=2},
                    new ReportData{Name = "WorkingHours_SessionsTotal", Data = UserWeeklyInfo.WorkingHours_SessionsTotal, Description = languageDataReport.Indicators.WorkingHours_SessionsTotal, ColourData=UserWeeklyInfo.WorkingHours_SessionsTotal.totalAvg, NotPercentage=true, ReportStyle=1},
                    new ReportData{Name = "AvgDialogueTime", Data = UserWeeklyInfo.AvgDialogueTime, Description = languageDataReport.Indicators.AvgDialogueTime, ColourData=UserWeeklyInfo.AvgDialogueTime.totalAvg},                    
                    new ReportData{Name = "CrossPhrase", Data = UserWeeklyInfo.CrossPhrase, Description = languageDataReport.Indicators.CrossPhrase, Thumbnail = true, ColourData=UserWeeklyInfo.CrossPhrase.totalAvg},
                    new ReportData{Name = "AlertPhrase", Data = UserWeeklyInfo.AlertPhrase, Description = languageDataReport.Indicators.AlertPhrase, ColourData=UserWeeklyInfo.AlertPhrase.totalAvg},
                    new ReportData{Name = "LoyaltyPhrase", Data = UserWeeklyInfo.LoyaltyPhrase, Description = languageDataReport.Indicators.LoyaltyPhrase, ColourData=UserWeeklyInfo.LoyaltyPhrase.totalAvg},
                    new ReportData{Name = "NecessaryPhrase", Data = UserWeeklyInfo.NecessaryPhrase, Description = languageDataReport.Indicators.NecessaryPhrase, ColourData=UserWeeklyInfo.NecessaryPhrase.totalAvg},
                    new ReportData{Name = "FillersPhrase", Data = UserWeeklyInfo.FillersPhrase, Description = languageDataReport.Indicators.FillersPhrase, ColourData=UserWeeklyInfo.FillersPhrase.totalAvg}
                };
            Dictionary<DateTime, float> points;
            List<string> base64Images = new List<string>();
            foreach(var item in parameters)
            {
                points = new Dictionary<DateTime, float>();
                foreach(var avg in item.Data.avgPerDay)
                {
                    points.Add(avg.Key, (float)avg.Value);                    
                }
                
                byte[] imageBytes = GeneratePng(points, item.Name, item.ColourData, item.ReportStyle).ToArray();
                item.Base64Image = Convert.ToBase64String(imageBytes);                
            }

            var fullName = applicationUser.FullName;
            
            var model = new ViewLanguageDataReport
            {
                ApplicationUserName = fullName,
                Parameters = parameters,
                LanguageDataReport = languageDataReport                
            };

            var templatePath = System.IO.Path.GetFullPath("./static/template.cshtml");
            
            string result = await engine.CompileRenderAsync("./static/template.cshtml", model); 

            System.IO.File.WriteAllText($"./static/template.html", result);

            await SendHttpReport(applicationUser, model);
        }
       
        private async Task<UserWeeklyData> GetUserWeeklyData(ApplicationUser applicationUser)
        {
            var token = GetTokenForInfo();

            if(token =="" || token == null)
            {
                _log.Error($"SendUserAnalyticReport, token is empty!");
                return null;
            }           
            var userName = applicationUser.FullName;
            var url = $"https://slavehb.northeurope.cloudapp.azure.com/api/AnalyticWeeklyReport/User?applicationUserId={applicationUser.Id}";
            var request = WebRequest.Create(url);
            request.Headers.Add($"Authorization", $"Bearer {token}");
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json";
            var responce = await request.GetResponseAsync();
            using(Stream dataStream = responce.GetResponseStream())
            {
                StreamReader reader = new StreamReader(dataStream);
                var responceFromServer = reader.ReadToEnd();
                var userData = new UserWeeklyData
                {
                    Authorization = $"{responceFromServer}",
                    UserFullName = $"'{userName}'"
                };
                return userData;
            }
        }
        private string GetTokenForInfo()
        {
            var user = _context.ApplicationUsers.Include(p => p.Company)
                .Where(p => p.NormalizedEmail == _autorizationData.UserName.ToUpper())
                .FirstOrDefault();
            if (user.StatusId != _context.Statuss.FirstOrDefault(x => x.StatusName == "Active").StatusId) 
            {                
                return "";
            }         
            var UserLoginChecked = _loginService.CheckUserLogin(_autorizationData.UserName, _autorizationData.Password);
            if (UserLoginChecked)
            {            
                _loginService.SaveErrorLoginHistory(user.Id, "success");
                return _loginService.CreateTokenForUser(user, _autorizationData.Remember);
            }
            else
            {                
                return null;                
            }                            
        }
         
        private async Task SendHttpReport(ApplicationUser applicationUser, ViewLanguageDataReport model)
        {                         
            var email = applicationUser.Email;
            if(email == "" || email == null)
            {
                _log.Error($"ApplicationUser {applicationUser.Id} email is empty");
                return;
            }     
            
            var beginWeek = DateTime.Now.AddDays(-7).ToString("dd/MM/yyyy");
            var endWeek = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");

            var mail = new System.Net.Mail.MailMessage();
            mail.From = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail);            
            
            mail.To.Add(email);            
            mail.Subject = model.LanguageDataReport.ReportName + " " + beginWeek + " - " + endWeek;
            mail.IsBodyHtml = true; 
            
            string htmlBody = File.ReadAllText("static/template.html");
            var htmlFileFullPath = Path.GetFullPath("static/template.html");
            System.Net.Mail.AlternateView alternateView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);   
                        
            mail.AlternateViews.Add(alternateView); 
            try
            {
                _smtpClient.SendAsync(mail);       
                _log.Info($"SendUserAnalyticReport, Mail Sent to application user: {applicationUser.Id} - {email}");        
            }
            catch(Exception ex)
            {
                _log.Fatal($"Failed send email to {applicationUser.Id} - {email}:\n{ex.Message}");
            }           
            System.Console.WriteLine($"Mail Sent");
            File.Delete(System.IO.Path.GetFullPath(htmlFileFullPath));
        }

        private MemoryStream GeneratePng(Dictionary<DateTime, float> points, string name, double colourData, int ReportStyle)
        {            
            float leftMargin = 10;
            float rightMargin = 10;
            float topMargin = 10;
            float bottomMargin = 5;
            float width = 400;
            float height = 100;
            var pane = new ZedGraph.GraphPane(new RectangleF(leftMargin, topMargin, width - leftMargin - rightMargin, height - topMargin - bottomMargin), "", "", "");
            pane.CurveList.Clear();
            PointPairList list = new PointPairList();
            int xmax = 100;
            int pointsStep = xmax/6;
            int counter = 0;

            
            float item = 0;
            foreach(KeyValuePair<DateTime, float> pair in points)
            {                         
                item = pair.Value;  
                list.Add(counter * pointsStep, item);
                counter++;
            }       
            LineItem myCurve;
            if(ReportStyle == 1)
            {
                if(colourData<10)
                {
                    myCurve = pane.AddCurve("", list, System.Drawing.ColorTranslator.FromHtml("#d8737c"), SymbolType.Circle);
                    myCurve.Line.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#d8737c");
                    myCurve.Symbol.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#d8737c");
                }
                else if(colourData>=10 && colourData<20)
                {
                    myCurve = pane.AddCurve("", list, System.Drawing.ColorTranslator.FromHtml("#FFA500"), SymbolType.Circle);
                    myCurve.Line.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#FFA500");
                    myCurve.Symbol.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#FFA500");
                }
                else
                {
                    myCurve = pane.AddCurve("", list, System.Drawing.ColorTranslator.FromHtml("#2ab978"), SymbolType.Circle);                
                    myCurve.Line.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#2ab978");                
                    myCurve.Symbol.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#2ab978");                
                }
            }
            else if(ReportStyle == 2)
            {
                myCurve = pane.AddCurve("", list, System.Drawing.ColorTranslator.FromHtml("#dedfd9"), SymbolType.Circle);
                myCurve.Line.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#dedfd9");
                myCurve.Symbol.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#dedfd9");
            }
            else
            {
                if(colourData<25)
                {
                    myCurve = pane.AddCurve("", list, System.Drawing.ColorTranslator.FromHtml("#d8737c"), SymbolType.Circle);
                    myCurve.Line.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#d8737c");
                    myCurve.Symbol.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#d8737c");
                }
                else if(colourData>=25 && colourData<50)
                {
                    myCurve = pane.AddCurve("", list, System.Drawing.ColorTranslator.FromHtml("#FFA500"), SymbolType.Circle);
                    myCurve.Line.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#FFA500");
                    myCurve.Symbol.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#FFA500");
                }
                else
                {
                    myCurve = pane.AddCurve("", list, System.Drawing.ColorTranslator.FromHtml("#2ab978"), SymbolType.Circle);                
                    myCurve.Line.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#2ab978");                
                    myCurve.Symbol.Fill.Color = System.Drawing.ColorTranslator.FromHtml("#2ab978");                
                }
            }
            
            myCurve.Line.IsVisible = true;
            myCurve.Line.Width = 2.0F;
            myCurve.Line.Fill.Type = FillType.GradientByY;
            myCurve.Symbol.Size = 7;


            pane.Margin.All = 0;
            pane.Legend.IsVisible = false;
            pane.Title.IsVisible = false;
            pane.XAxis.IsVisible = false;
            pane.YAxis.IsVisible = false;

            pane.Border.Width = 0;
            pane.Border.Color = Color.White;
            pane.Border.IsVisible = false;
            pane.Border.IsAntiAlias = true;
            
            pane.Chart.Border.IsVisible = false;
            pane.Legend.Border.IsVisible = false;
            pane.Legend.FontSpec.Fill.IsVisible = false;
            pane.Legend.Fill.IsVisible = false;
            pane.Legend.IsVisible = false;
            pane.Title.IsVisible = false;

            pane.XAxis.Scale.Max = 100;
            pane.XAxis.Scale.Min = 0;

            pane.YAxis.Scale.Max = 2*points.Values.Max();
            pane.YAxis.Scale.Min = 0;
            pane.AxisChange();            
            
            MemoryStream imageMemoryStream = new MemoryStream();
            pane.GetImage().Save(imageMemoryStream, System.Drawing.Imaging.ImageFormat.Png);
            imageMemoryStream.Position = 0;
            return imageMemoryStream;
        }   
    }       
}
