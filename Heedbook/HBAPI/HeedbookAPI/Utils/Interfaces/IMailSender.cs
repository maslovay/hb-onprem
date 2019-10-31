using HBData.Models;
using System.Threading.Tasks;

namespace UserOperations.Services
{
    public interface IMailSender
    {
        void SendSimpleEmail(string email, string messageTitle, string text, string senderName = "Heedbook");
        Task SendRegisterEmail(ApplicationUser user);

        Task SendUserRegisterEmail(ApplicationUser user, string password);

        Task SendPasswordChangeEmail(ApplicationUser user, string password);
    }
}
