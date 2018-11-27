using System;
using HBLib.Utils;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace TEST_Onpremhelloworld
{
    class Program
    {
        struct JSONMessage
        {
            public string Action;
            public string FileName;
            public string Message;
        };

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Start initialisation.");
                var sets = new HeedbookMessengerStatic.MQMessenger.MQSettings()
                {
                    HostName = "40.113.133.225",
                    Password = "hbhbhbhbhb",
                    UserName = "hbadmin",
                    VHost = "hbrabbit"
                };
                HeedbookMessengerStatic.MQMessenger.Init(sets);
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Connection initialized.");
                string topic = "test-topic";
                HeedbookMessengerStatic.MQMessenger.Subscribe(topic, (obj, msg) => Worker(msg));
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Subscribed to {topic}.");
                string s = "123";
                //while (!(s is null) && (s != "q"))
                while(true)
                {
                    //Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Print \"q\" to exit.");
                    //s = Console.ReadLine();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e);
            }
            finally
            {
                HeedbookMessengerStatic.MQMessenger.Dispose();
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Press any key to exit.");
                Console.ReadKey();
            }
        }

        static void Worker(string message)
        {
            try
            {
                //var rawmsg = JsonConvert.DeserializeObject(message, typeof(JSONMessage));
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Current directory is {Directory.GetCurrentDirectory()}");
                JSONMessage msg = (JSONMessage)JsonConvert.DeserializeObject(message, typeof(JSONMessage));
                switch (msg.Action)
                {
                    case "Create":
                        if (File.Exists(msg.FileName))
                        {
                            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Error: file \"{msg.FileName}\" already exists.");
                            return;
                        }
                        var file = File.Create(msg.FileName);
                        file.Write(System.Text.Encoding.UTF8.GetBytes(msg.Message), 0, System.Text.Encoding.UTF8.GetBytes(msg.Message).Length);
                        file.Close();
                        Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Created file \"{file.Name}\".");
                        break;
                    case "Delete":
                        if (!File.Exists(msg.FileName))
                        {
                            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Error: file \"{msg.FileName}\" not exists.");
                            return;
                        }
                        try
                        {
                            File.Delete(msg.FileName);
                            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Deleted file \"{msg.FileName}\".");
                        }
                        catch
                        {
                            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Error: couldn't delete file \"{msg.FileName}\".");
                        }
                        break;
                    default:
                        Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Error: unknown action \"{msg.Action}\".");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Exception: {e}");
            }
        }
    }
}
