using System;
using System.Collections.Generic;
using System.Threading;
using AlarmSender.DataStructures;
using Telegram.Bot.Types;

namespace AlarmSender
{
    public abstract class Sender : ISender
    {
        protected List<AlarmSenderChat> Chats  => new List<AlarmSenderChat>(3);

        public delegate void CommandReceivedDelegate(string command);

        public event CommandReceivedDelegate CommandReceived;

        public abstract void Send(string message, string chatName, bool processCallback = true);

        public void ReceiveCommands()
        {
            for (;;)
            {
                try
                {
                    foreach (var chat in Chats)
                    {
                        var pollResult = Poll(chat.Name);
                        if (string.IsNullOrWhiteSpace(pollResult)) 
                            continue;
                        Console.WriteLine("Poll result: " + pollResult);
                        Console.WriteLine("CommandReceived event: " + CommandReceived?.ToString());

                        CommandReceived?.Invoke(pollResult);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Sender.ReceiveCommands() exception occurred: " + ex.Message + " : " + ex.StackTrace);
                }
            }
        }

        protected abstract string Poll(string chatName);
    }
}