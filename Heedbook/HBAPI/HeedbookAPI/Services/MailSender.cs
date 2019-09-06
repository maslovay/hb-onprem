using HBData.Models;
using HBLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using HBLib.Utils;
using Newtonsoft.Json;
using System.Net.Mime;

namespace UserOperations.Services
{
    public class MailSender
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;
        private readonly ElasticClient _log;
        public MailSender(SmtpSettings smtpSettings, SmtpClient smtpClient, ElasticClient log)
        {
            _smtpSettings = smtpSettings;
            _smtpClient = smtpClient;
            _log = log;
        }

        public void SendSimpleEmail(string email, string messageTitle, string text, string senderName = "Heedbook")
        {
            SendOldEmail(email, messageTitle, text);
        }

        public async Task SendRegisterEmail(ApplicationUser user)
        {
            LanguageDataEmail model = await ReadLanguageModel(user, "registerManager");
            string htmlBody = await CreateHtmlFromTemplate(model, "email.cshtml");
            await SendEmail(user, model.emailSubject, htmlBody);
        }

        public async Task SendUserRegisterEmail(ApplicationUser user, string password)
        {
            LanguageDataEmail model = await ReadLanguageModel(user, "registerUser");
            model.greeting += user.FullName;
            model.pswd += password;
            model.login = user.Email;
            string htmlBody = await CreateHtmlFromTemplate(model, "email.cshtml");
            await SendEmail(user, model.emailSubject, htmlBody);
        }

        public async Task SendPasswordChangeEmail(ApplicationUser user, string password)
        {
            LanguageDataEmail model = await ReadLanguageModel(user, "passwordChange");
            model.greeting += user.FullName;
            model.pswd += password;
            string htmlBody = await CreateHtmlFromTemplate(model, "email.cshtml");
            await SendEmail(user, model.emailSubject, htmlBody);
           // SendOldEmail(email, "Password changed", text);
        }

        public async Task SendOldEmail(string email, string subject, string text)
        {
            System.Net.Mail.MailAddress from = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail, "Heedbook");
            System.Net.Mail.MailAddress to = new System.Net.Mail.MailAddress(email);
            // create mail object 
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage(from, to);
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.IsBodyHtml = true;
            mail.Subject = subject;
            mail.Body = text;
            try
            {
                _smtpClient.Send(mail);
                //  _log.Info($"email Sended to {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //  _log.Fatal($"Failed email to {email}{ex.Message}");
            }
        }

        //create and email notification 
        private async Task SendEmail(ApplicationUser user, string subject, string htmlBody)
        {
            System.Net.Mail.MailAddress from = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail, "Heedbook");
            System.Net.Mail.MailAddress to = new System.Net.Mail.MailAddress(user.Email);
            // create mail object 
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage(from, to);
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.IsBodyHtml = true;           
            mail.Subject = subject;
            mail.Body = htmlBody;
            try
            {
                _smtpClient.Send(mail);
              //  _log.Info($"email Sended to {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
              //  _log.Fatal($"Failed email to {email}{ex.Message}");
            }
        }   

        private async Task<LanguageDataEmail> ReadLanguageModel(ApplicationUser user, string emailType)
        {
            try
            {
                var languageId = user.Company.LanguageId;
                string path = Path.GetFullPath("./Services/language_table.json");
                var languageRowJson = File.ReadAllText(path);

                var languageObject = JsonConvert.DeserializeObject<EmailModel>(languageRowJson);
                var registerLanguages = (List<LanguageDataEmail>)languageObject.GetType().GetProperty(emailType).GetValue(languageObject, null);
                //var registerLanguages = languageObject.register;
                if (languageId - 1 > registerLanguages.Count)
                    languageId = 1;
                return registerLanguages[languageId == null ? 0 : (int)languageId - 1];              
            }
            catch (Exception ex)
            {
                _log.Fatal($"Read Languages fatal {ex.Message}");
                return null;
            }
        }

        private async Task<string> CreateHtmlFromTemplate(LanguageDataEmail model, string filename)
        {
            try
            {
                var fullPath = System.IO.Path.GetFullPath(".");
                var engine = new RazorLight.RazorLightEngineBuilder()
                    .UseFilesystemProject(fullPath)
                    .UseMemoryCachingProvider()
                    .Build();
            //    string path = Path.GetFullPath("./Services /"+filename);
                string result = await engine.CompileRenderAsync("./Services/"+filename, model);

                string pathTemp = fullPath + "/Services/temp.html";
                File.WriteAllText(pathTemp, result);
                string htmlBody = File.ReadAllText(pathTemp);
                File.Delete(pathTemp);
                return htmlBody;
            }
            catch (Exception ex)
            {
                _log.Fatal($"Create user email fatal exception {ex.Message}");
                return "";
            }
        }


    }
        public class LanguageDataEmail
        {
            public string language { get; set; }
            public string emailSubject { get; set; }
            public string greeting { get; set; }
            public string body { get; set; }
            public string button { get; set; }
            public string text1 { get; set; }
            public string text2 { get; set; }
            public string text3 { get; set; }
            public string text4 { get; set; }
            public string text5 { get; set; }
            public string footer { get; set; }
            public string login { get; set; }
            public string pswd { get; set; }

    }
    public class EmailModel
        {
            //public string userEmail { get; set; }
            public string userName { get; set; }
            public string password { get; set; }
            public List<LanguageDataEmail> registerManager { get; set; }
            public List<LanguageDataEmail> registerUser { get; set; }
            public List<LanguageDataEmail> passwordChange { get; set; }
    }
}
