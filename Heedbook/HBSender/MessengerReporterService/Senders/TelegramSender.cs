using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using MessengerReporterService.Models;
using Microsoft.Extensions.Configuration;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MessengerReporterService.Senders
{
    public class TelegramSender : Sender
    {
        private readonly IConfiguration _configuration;
        private readonly List<string> _commands = new List<string>(10);
        private int updatesOffset = 0;
        private readonly object syncObj = new object();
        private readonly INotificationPublisher _publisher;

        public TelegramSender(/*ILogger logger, */ IConfiguration configuration, INotificationPublisher publisher)
        {
            //_logger = logger;
            _configuration = configuration;

            var chatSections = _configuration.GetSection("AlarmSender").GetSection("Chats").GetChildren().ToArray();
            _publisher = publisher;
            
            CommandReceived += SendCommand;
            foreach (var section in chatSections)
            {
                try
                {
                    var token = section.GetSection("Telegram").GetValue<string>("Token");
                    var chatId = section.GetSection("Telegram").GetValue<string>("ChatId");
                    var client = ((TelegramChat) (Chats.FirstOrDefault(c => (c is TelegramChat tc && tc.Token == token))))?.Client;
                    var chat = new TelegramChat(section.Key, 
                    chatId,
                    token,
                    client);

                    Chats.Add(chat);   
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }
                             
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
        private void SendCommand(string command)
        {
            var message = new IntegrationTestsRun()
            {
                Command = command
            };
            _publisher.Publish(message);
        }

        protected override string Poll(AlarmSenderChat chat)
        {
            try
            {
                lock (syncObj)
                {
                    if (!(Chats.FirstOrDefault(c => c.Name == chat.Name) is TelegramChat _chat))
                        return string.Empty;

                    //_client.StartReceiving();
                    while (_chat.Client.IsReceiving)
                        Thread.Sleep(1);
                    var updateTask = _chat.Client.GetUpdatesAsync(updatesOffset);
                    updateTask.Wait();
                    //_client.StopReceiving();
                    
                    var orderedResults = updateTask.Result?.OrderByDescending(u => u.Id);
                    var lastMessageId = orderedResults?.FirstOrDefault(r => r.ChannelPost.Text.Contains("/"))?.ChannelPost.MessageId;
                    var command = orderedResults?.FirstOrDefault(r => r.ChannelPost.Text.Contains("/"))?.ChannelPost.Text;                    
                    var chatId = orderedResults?.FirstOrDefault(r => r.ChannelPost.Text.Contains("/"))?.ChannelPost?.Chat?.Id;

                    var callbackId = orderedResults?.FirstOrDefault(r => r.CallbackQuery != null)?.CallbackQuery.Id;
                    if (callbackId != null)
                        _chat.Client.AnswerCallbackQueryAsync(callbackId).Wait();

                    updatesOffset = orderedResults?.FirstOrDefault()?.Id ?? 0;
                    ++updatesOffset;

                    if (string.IsNullOrEmpty(command) && _chat.LastMessageId == lastMessageId)
                        return string.Empty;
                    _chat.LastMessageId = lastMessageId;
                    System.Console.WriteLine($"MessageId: {lastMessageId}");
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
