using HBData.Models;
using HBLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using HBLib.Utils;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using UserOperations.Models;
using UserOperations.Utils.CommonOperations;

namespace UserOperations.Services
{
    public class MailSender : IMailSender
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;
        private readonly FileRefUtils _fileRef;
        private readonly string _localFolder;
        private readonly string _containerName;
        public MailSender(SmtpSettings smtpSettings, SmtpClient smtpClient, FileRefUtils fileRef)
        {
            _smtpSettings = smtpSettings;
            _smtpClient = smtpClient;
            _fileRef = fileRef;
            _localFolder = @"/Utils/CommonOperations/";
            _containerName = "media";
        }

        public void SendSimpleEmail(string email, string messageTitle, string text, string senderName = "Heedbook")
        {
            System.Net.Mail.MailAddress from = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail, "Heedbook");
            System.Net.Mail.MailAddress to = new System.Net.Mail.MailAddress(email);
            // create mail object 
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage(from, to)
            {
                BodyEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = true,
                Subject = messageTitle,
                Body = text
            };
            try
            {
                _smtpClient.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendsEmailsSubscription(IFormCollection formData, ApplicationUser user, VideoMessage message, List<ApplicationUser> recepients)
        {
            var mail = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail),
                Subject = user.FullName + " - " + message.Subject,
                Body = message.Body,
                IsBodyHtml = false
            };

            foreach (var r in recepients)
            {
                mail.To.Add(r.Email);
            }

            var amountAttachmentsSize = 0f;
            foreach (var f in formData.Files)
            {
                var fn = user.FullName + "_" + formData.Files[0].FileName;
                var memoryStream = f.OpenReadStream();
                amountAttachmentsSize += (memoryStream.Length / 1024f) / 1024f;

                memoryStream.Position = 0;
                var attachment = new System.Net.Mail.Attachment(memoryStream, fn);
                mail.Attachments.Add(attachment);
            }
            if (amountAttachmentsSize > 25)
            {
                throw new Exception($"Files size more than 25 MB");
            }

            try
            {
                _smtpClient.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task SendRegisterEmail(ApplicationUser user)
        {
            LanguageDataEmail model = await ReadLanguageModel(user, "RegisterManager");
            model.FileRef = _fileRef.GetFileUrlFast(_containerName);
            string htmlBody = await CreateHtmlFromTemplate(model, "email.cshtml");
            await SendEmail(user, model.EmailSubject, htmlBody);
        }

        public async Task SendUserRegisterEmail(ApplicationUser user, string password)
        {
            LanguageDataEmail model = await ReadLanguageModel(user, "RegisterUser");
            model.FileRef = _fileRef.GetFileUrlFast(_containerName);
            model.Greeting += user.FullName;
            model.Pswd += password;
            model.Login = user.Email;
            string htmlBody = await CreateHtmlFromTemplate(model, "email.cshtml");
            await SendEmail(user, model.EmailSubject, htmlBody);
        }

        public async Task SendPasswordChangeEmail(ApplicationUser user, string password)
        {
            LanguageDataEmail model = await ReadLanguageModel(user, "PasswordChange");
            model.FileRef = _fileRef.GetFileUrlFast(_containerName);
            model.Greeting += user.FullName;
            model.Pswd += password;
            string htmlBody = await CreateHtmlFromTemplate(model, "email.cshtml");
            await SendEmail(user, model.EmailSubject, htmlBody);
        }

        //create and email notification 
        private async Task SendEmail(ApplicationUser user, string subject, string htmlBody)
        {
            System.Net.Mail.MailAddress from = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail, "Heedbook");
            System.Net.Mail.MailAddress to = new System.Net.Mail.MailAddress(user.Email);
            // create mail object 
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage(from, to)
            {
                BodyEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = true,
                Subject = subject,
                Body = htmlBody
            };
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
                string path = Path.GetFullPath("." + _localFolder + "language_table.json");
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
                return null;
            }
        }

        private async Task<string> CreateHtmlFromTemplate(LanguageDataEmail model, string filename)
        {
            try
            {
                var fullPath = System.IO.Path.GetFullPath(".");
                //var engine = new RazorLight.RazorLightEngineBuilder()
                //    .UseFilesystemProject(fullPath)
                //    .UseMemoryCachingProvider()
                //    .Build();
                //string result = await engine.CompileRenderAsync(folder + "email", model);

                var engine = new RazorLight.RazorLightEngineBuilder()
              .UseMemoryCachingProvider()
              .Build();

                string template = File.ReadAllText(fullPath + _localFolder + "email.cshtml");
                string result = await engine.CompileRenderAsync("email", template, model);

                string pathTemp = fullPath + _localFolder + "temp.html";
                File.WriteAllText(pathTemp, result);
                string htmlBody = File.ReadAllText(pathTemp);
                File.Delete(pathTemp);
                return htmlBody;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<string> TestReadFile1()
        {
            string path = Path.GetFullPath("." + _localFolder + "language_table.json");
            var languageRowJson = File.ReadAllText(path);
            var languageObject = JsonConvert.DeserializeObject<EmailModel>(languageRowJson);
            var registerLanguages = (List<LanguageDataEmail>)languageObject.GetType().GetProperty("passwordChange").GetValue(languageObject, null);
            LanguageDataEmail model = registerLanguages[1];
            try
            {
                var fullPath = System.IO.Path.GetFullPath(".");
                var engine = new RazorLight.RazorLightEngineBuilder()
                    .UseFilesystemProject(fullPath)
                    .UseMemoryCachingProvider()
                    .Build();
                try
                {
                    string result = await engine.CompileRenderAsync("Utils/CommonOperations/email.cshtml", model);
                    string pathTemp = fullPath + _localFolder + "temp.html";
                    File.WriteAllText(pathTemp, result);
                    string htmlBody = File.ReadAllText(pathTemp);
                    File.Delete(pathTemp);
                    return htmlBody;
                }
                catch (Exception ex)
                {
                    return "engine.CompileRenderAsync dosnt work. " + ex.Message + ", " + ex.StackTrace;
                }

            }
            catch (Exception ex)
            {
                return ex.Message + ex.InnerException?.Message;
            }
        }

        public async Task<string> TestReadFile2()
        {
            string path = Path.GetFullPath("." + _localFolder + "language_table.json");
            var languageRowJson = File.ReadAllText(path);
            var languageObject = JsonConvert.DeserializeObject<EmailModel>(languageRowJson);
            var registerLanguages = (List<LanguageDataEmail>)languageObject.GetType().GetProperty("passwordChange").GetValue(languageObject, null);
            LanguageDataEmail model = registerLanguages[1];
            try
            {
                var engine = new RazorLight.RazorLightEngineBuilder()
                  .UseMemoryCachingProvider()
                  .Build();
                var fullPath = System.IO.Path.GetFullPath(".");
                string template = File.ReadAllText(fullPath + _localFolder + "email.cshtml");

                string result = await engine.CompileRenderAsync("email", template, model);
                string pathTemp = fullPath + _localFolder + "temp.html";
                File.WriteAllText(pathTemp, result);
                string htmlBody = File.ReadAllText(pathTemp);
                File.Delete(pathTemp);
                return htmlBody;
            }
            catch (Exception ex)
            {
                return ex.Message + " , " + ex.StackTrace;
            }
        }
    }
    public class LanguageDataEmail
        {
            public string FileRef { get; set; }
            public string Language { get; set; }
            public string EmailSubject { get; set; }
            public string Greeting { get; set; }
            public string Body { get; set; }
            public string Button { get; set; }
            public string Text1 { get; set; }
            public string Text2 { get; set; }
            public string Text3 { get; set; }
            public string Text4 { get; set; }
            public string Text5 { get; set; }
            public string Footer { get; set; }
            public string Login { get; set; }
            public string Pswd { get; set; }

    }
    public class EmailModel
        {
            //public string userEmail { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public List<LanguageDataEmail> RegisterManager { get; set; }
            public List<LanguageDataEmail> RegisterUser { get; set; }
            public List<LanguageDataEmail> PasswordChange { get; set; }
    }
}
