using HBData.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using UserOperations.Models;

namespace UserOperations.Services
{
    public interface IMailSender
    {
        Task SendPasswordChangeEmail(ApplicationUser user, string password);
        Task SendRegisterEmail(ApplicationUser user);
        void SendsEmailsSubscription(IFormCollection formData, ApplicationUser user, VideoMessage message, List<ApplicationUser> recepients);
        void SendSimpleEmail(string email, string messageTitle, string text, string senderName = "Heedbook");
        Task SendUserRegisterEmail(ApplicationUser user, string password);
        Task<string> TestReadFile1();
        Task<string> TestReadFile2();
    }
}