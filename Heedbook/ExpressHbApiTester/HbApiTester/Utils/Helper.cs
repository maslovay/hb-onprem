using System;
using System.Collections.Generic;
using System.Linq;
using AlarmSender;
using HbApiTester.Settings;
using Microsoft.Extensions.Configuration;
using NLog;

namespace HbApiTester.Utils
{
    public static class Helper
    {
        public static object syncObj = new object();
        public static void FetchSenders(ILogger logger, RunnerSettings settings, ICollection<Sender> senders, IServiceProvider serviceProvider)
        {
            lock (syncObj)
            {
                try
                {
                    // logger.Info("Loading senders...");
                    Console.WriteLine("Loading senders...");
                    if (!settings.Handlers.Any())
                    {
                        Console.WriteLine("No senders in config!");
                        //logger.Error("No senders in config!");
                        return;
                    }

                    foreach (var handler in settings.Handlers)
                    {
                        Console.WriteLine($"Loading senders... {handler}");
                        switch (handler)
                        {
                            default:
                            case "Telegram":
                                if (senders.All(s => s.GetType() != typeof(TelegramSender)))
                                    senders.Add((TelegramSender)serviceProvider.GetService(typeof(TelegramSender)));
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Helper.FetchSenders() exception: " + ex.Message);
                }
            }
        }
    }
}