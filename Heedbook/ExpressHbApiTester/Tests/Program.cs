using System;
using System.Net;
using AlarmSender.DataStructures;
using Newtonsoft.Json;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var reply = "{\"ok\":true,\"result\":[{\"update_id\":725025169,"+
                "\"channel_post\":{\"message_id\":19178,\"chat\":{\"id\":-1001238862306,\"title\":\"HeedbookAlarms\",\"type\":\"channel\"},\"date\":1563448928,\"text\":\"/fff\",\"entities\":[{\"offset\":0,\"length\":4,\"type\":\"bot_command\"}]}},{\"update_id\":725025170,"+
                "\"channel_post\":{\"message_id\":19179,\"chat\":{\"id\":-1001238862306,\"title\":\"HeedbookAlarms\",\"type\":\"channel\"},\"date\":1563449125,\"text\":\"Fhj\"}},{\"update_id\":725025171,"+
                "\"channel_post\":{\"message_id\":19180,\"chat\":{\"id\":-1001238862306,\"title\":\"HeedbookAlarms\",\"type\":\"channel\"},\"date\":1563449145,\"text\":\"/getupdates\",\"entities\":[{\"offset\":0,\"length\":11,\"type\":\"bot_command\"}]}}]}";

            var updateResponse = JsonConvert.DeserializeObject<TelegramUpdateResponse>(reply);
                
            Console.WriteLine("Poll(): updateResponse responses count = " + updateResponse.result.Count);

            
        }
    }
}