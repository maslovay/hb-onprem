using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using AlarmSender.DataStructures;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using NLog;

namespace AlarmSender
{
    public class TelegramSender : Sender
    {
        private readonly IConfiguration _configuration;
//        private readonly string _chatId;
//        private readonly string _token;
        //private readonly ILogger _logger;
        private readonly List<string> _commands = new List<string>(10);
        private int updatesOffset = 0;
        private readonly object syncObj = new object();
//        private TelegramBotClient _client;
        
        public TelegramSender(/*ILogger logger, */ IConfiguration configuration)
        {
            //_logger = logger;
            _configuration = configuration;

            var chatSections = _configuration.GetSection("AlarmSender").GetChildren().ToArray();

            foreach (var section in chatSections)
            {
                var chat = new TelegramChat(section.Key, 
                    section.GetSection("Telegram").GetValue<string>("ChatId"),
                    section.GetSection("Telegram").GetValue<string>("Token"));
                
                Chats.Add(chat);
            }

            var receiveThrd = new Thread(ReceiveCommands);
            receiveThrd.Start();
        }

        public override async void Send(string message, string chatName, bool processCallback = true)
        {
            if (!(Chats.FirstOrDefault(c => c.Name == chatName) is TelegramChat chat))
                return;
            
            await chat.Client?.SendTextMessageAsync(chat.ChatId, message, ParseMode.Html);
            if (processCallback == true)
                ProcessCallbackImmediately(chat);
        }

        private void ProcessCallbackImmediately(TelegramChat chat)
        {
            try
            {
                lock (syncObj)
                {
                    while (chat.Client.IsReceiving)
                        Thread.Sleep(1);
                    var updateTask = (chat.Client.GetUpdatesAsync(updatesOffset, 30, 8));
                    updateTask.Wait();
                    
                    var orderedResults = updateTask.Result?.OrderByDescending(u => u.Id);
                    var callbackId = orderedResults?.FirstOrDefault(r => r.CallbackQuery != null)?.CallbackQuery.Id;
                    if (callbackId != null)
                    {
                        Console.WriteLine("TelegramSender.ProcessCallbackImmediately Callback id = " + callbackId);
                        (chat.Client.AnswerCallbackQueryAsync(callbackId)).Wait();
                    }

                    updatesOffset = orderedResults?.FirstOrDefault()?.Id ?? 0;
                    ++updatesOffset;
                }
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("TelegramSender.ProcessCallbackImmediately() exception: " + ex.Message);
                chat.Client?.StopReceiving();
            }
        }

        protected override string Poll(string chatName)
        {
            try
            {
                lock (syncObj)
                {
                    if (!(Chats.FirstOrDefault(c => c.Name == chatName) is TelegramChat chat))
                        return string.Empty;

                    //_client.StartReceiving();
                    while (chat.Client.IsReceiving)
                        Thread.Sleep(1);
                    var updateTask = chat.Client.GetUpdatesAsync(updatesOffset, 10, 5);
                    updateTask.Wait();
                    //_client.StopReceiving();
                    
                    var orderedResults = updateTask.Result?.OrderByDescending(u => u.Id);
                    var command = orderedResults?.FirstOrDefault(r => r.ChannelPost.Text.Contains("/"))?.ChannelPost
                        .Text;
                    
                    var chatId = orderedResults?.FirstOrDefault(r => r.ChannelPost.Text.Contains("/"))?.ChannelPost?.Chat?.Id;

                    var callbackId = orderedResults?.FirstOrDefault(r => r.CallbackQuery != null)?.CallbackQuery.Id;
                    if (callbackId != null)
                        chat.Client.AnswerCallbackQueryAsync(callbackId).Wait();

                    updatesOffset = orderedResults?.FirstOrDefault()?.Id ?? 0;
                    ++updatesOffset;

                    if (string.IsNullOrEmpty(command))
                        return string.Empty;

                    Console.WriteLine("Chat id: " + chatId);
                    Console.WriteLine("Command found: " + command);
                    return command.Replace("/", "");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TelegramSender.Poll() exception: " + ex.Message);
            }
            finally
            {
                Thread.Sleep(500);
            }

            return string.Empty;
        }
    }
}
