using System;
using System.Threading;
using Telegram.Bot.Types;

namespace AlarmSender
{
    public abstract class Sender : ISender
    {
        public delegate void CommandReceivedDelegate(string command);

        public event CommandReceivedDelegate CommandReceived;

        public abstract void Send(string message, bool processCallback = true);

        public void ReceiveCommands()
        {
            for (;;)
            {
                try
                {
                    var pollResult = Poll();
                    if (!string.IsNullOrWhiteSpace(pollResult))
                    {
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

        protected abstract string Poll();
    }
}