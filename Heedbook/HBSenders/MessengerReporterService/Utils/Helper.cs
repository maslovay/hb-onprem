using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NLog;
using MessengerReporterService.Models;
using MessengerReporterService.Senders;
using Newtonsoft.Json;

namespace MessengerReporterService.Utils
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
                    Console.WriteLine($"Loading senders...: {senders.Count}");
                    if (!settings.Handlers.Any())
                    {
                        Console.WriteLine("No senders in config!");
                        //logger.Error("No senders in config!");
                        return;
                    }

                    foreach (var handler in settings.Handlers)
                    {
                        switch (handler)
                        {
                            default:
                            case "Telegram":
                                if (senders.All(s => s.GetType() != typeof(TelegramSender)))
                                    senders.Add((TelegramSender)serviceProvider.GetService(typeof(TelegramSender)));
                                break;
                        }
                    }
                    Console.WriteLine($"Loaded senders...: {senders.Count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Helper.FetchSenders() exception: " + ex.Message);
                }
            }
        }
    }
}