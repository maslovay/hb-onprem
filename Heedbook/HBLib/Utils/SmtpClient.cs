using System;
using System.Net;
using HBLib;


namespace HBLib.Utils
{
    public class SmtpClient : IDisposable
    {
        private System.Net.Mail.SmtpClient _smtpClient;
        private readonly SmtpSettings _smtpSettings;
        public SmtpClient(SmtpSettings smtpSettings)
        {
            _smtpSettings = smtpSettings;
            _smtpClient = new System.Net.Mail.SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
        }

        public void Send(System.Net.Mail.MailMessage mail)
        {
            _smtpClient = new System.Net.Mail.SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
            _smtpClient.DeliveryMethod = (System.Net.Mail.SmtpDeliveryMethod)_smtpSettings.DeliveryMethod;
            _smtpClient.EnableSsl = _smtpSettings.EnableSsl;
            _smtpClient.UseDefaultCredentials = _smtpSettings.UseDefaultCredentials;
            _smtpClient.Timeout = _smtpSettings.Timeout;
            _smtpClient.Credentials = new NetworkCredential(_smtpSettings.FromEmail, _smtpSettings.Password);  
            try
            {                
                _smtpClient.Send(mail);                          
            }
            catch(Exception ex)
            {
                throw;                
            }
        }

        public async void SendAsync(System.Net.Mail.MailMessage mail)
        {
            _smtpClient = new System.Net.Mail.SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
            _smtpClient.DeliveryMethod = (System.Net.Mail.SmtpDeliveryMethod)_smtpSettings.DeliveryMethod;
            _smtpClient.EnableSsl = _smtpSettings.EnableSsl;
            _smtpClient.UseDefaultCredentials = _smtpSettings.UseDefaultCredentials;
            _smtpClient.Timeout = _smtpSettings.Timeout;
            _smtpClient.Credentials = new NetworkCredential(_smtpSettings.FromEmail, _smtpSettings.Password);  
            try
            {                
                await _smtpClient.SendMailAsync(mail);                          
            }
            catch(Exception ex)
            {
                throw;                
            }
        }

        public void Dispose()
        {
            _smtpClient.Dispose();            
        }

        protected virtual void Dispose(bool disposing)
        {
            _smtpClient.Dispose();
        }
    }

}