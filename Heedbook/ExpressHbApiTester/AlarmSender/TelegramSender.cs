using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using NLog;

namespace AlarmSender
{
    public class TelegramSender : Sender
    {
        private readonly IConfiguration _configuration;
        private readonly string _chatId;
        private readonly string _token;
        //private readonly ILogger _logger;
        private readonly List<string> _commands = new List<string>(10);
        private int updatesOffset = 0;
        private readonly object syncObj = new object();
        private TelegramBotClient _client;
        
        public TelegramSender(/*ILogger logger, */ IConfiguration configuration)
        {
            //_logger = logger;
            _configuration = configuration;
            _token = _configuration.GetSection("AlarmSender").GetSection("Telegram").GetValue<string>("Token");
            _chatId = _configuration.GetSection("AlarmSender").GetSection("Telegram").GetValue<string>("ChatId");
            _client = new TelegramBotClient(_token);
            var receiveThrd = new Thread(ReceiveCommands);
            receiveThrd.Start();
        }

        public override async void Send(string message, bool processCallback = true)
        {
            await _client?.SendTextMessageAsync(_chatId, message, ParseMode.Html);
            if (processCallback == true)
                ProcessCallbackImmediately();
        }

        private void ProcessCallbackImmediately()
        {
            try
            {
                lock (syncObj)
                {
                    while (_client.IsReceiving)
                        Thread.Sleep(1);
                    var updateTask = (_client.GetUpdatesAsync(updatesOffset, 30, 8));
                    updateTask.Wait();
                    
                    var orderedResults = updateTask.Result?.OrderByDescending(u => u.Id);
                    var callbackId = orderedResults?.FirstOrDefault(r => r.CallbackQuery != null)?.CallbackQuery.Id;
                    if (callbackId != null)
                    {
                        Console.WriteLine("TelegramSender.ProcessCallbackImmediately Callback id = " + callbackId);
                        (_client.AnswerCallbackQueryAsync(callbackId)).Wait();
                    }

                    updatesOffset = orderedResults?.FirstOrDefault()?.Id ?? 0;
                    ++updatesOffset;
                }
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("TelegramSender.ProcessCallbackImmediately() exception: " + ex.Message);
                _client?.StopReceiving();
            }
        }

        protected override string Poll()
        {
            try
            {
                lock (syncObj)
                {
                    //_client.StartReceiving();
                    while (_client.IsReceiving)
                        Thread.Sleep(1);
                    var updateTask = (_client.GetUpdatesAsync(updatesOffset, 10, 5));
                    updateTask.Wait();
                    //_client.StopReceiving();
                    
                    var orderedResults = updateTask.Result?.OrderByDescending(u => u.Id);
                    var command = orderedResults?.FirstOrDefault(r => r.ChannelPost.Text.Contains("/"))?.ChannelPost
                        .Text;
                    var callbackId = orderedResults?.FirstOrDefault(r => r.CallbackQuery != null)?.CallbackQuery.Id;
                    if (callbackId != null)
                        (_client.AnswerCallbackQueryAsync(callbackId)).Wait();

                    updatesOffset = orderedResults?.FirstOrDefault()?.Id ?? 0;
                    ++updatesOffset;

                    if (string.IsNullOrEmpty(command))
                        return string.Empty;

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
                Thread.Sleep(5000);
            }

            return string.Empty;
        }
    }
}
