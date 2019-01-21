using System.Net.Http;
using System.Text;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;

namespace OperationService.Legacy
{
    public static class SlackScheduleExceptionNotifierTimer
    {
        [FunctionName("Slack_Schedule_ExceptionNotifierTimer")]
        public static void Run([TimerTrigger("0 */5 * * * *"), Disable()]
            TimerInfo myTimer)
        {
            HeedbookMessengerStatic.HttpClient.PostAsync(EnvVar.Get("SlackExceptionNotifierURL"),
                new StringContent("Timer", Encoding.UTF8, "application/json"));
        }
    }
}