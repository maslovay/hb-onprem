using System.Net;
using System.Net.Http;
using Telegram.Bot;

namespace AlarmSender.DataStructures
{
    public class TelegramChat : AlarmSenderChat
    {
        public TelegramChat(string chatName, string chatId, string token) : base(chatName)
        {
            ChatId = chatId;
            Token = token;
            Client = new TelegramBotClient(token, new HttpClient());
        }
        
        public string ChatId { get; private set; }
        public string Token { get; private set; }
        
        public TelegramBotClient Client { get;  set; }
    }
}