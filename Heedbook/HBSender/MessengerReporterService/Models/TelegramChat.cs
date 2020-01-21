using System.Net;
using System.Net.Http;
using Telegram.Bot;

namespace MessengerReporterService.Models
{
    public class TelegramChat : AlarmSenderChat
    {
        public TelegramChat(string chatName, string chatId, string token, ITelegramBotClient client = null) : base(chatName)
        {
            ChatId = chatId;
            Token = token;
            Client = client ?? new TelegramBotClient(token, new HttpClient());
        }
        
        public string ChatId { get; private set; }
        public string Token { get; private set; }
        
        public ITelegramBotClient Client { get;  set; }
    }
}