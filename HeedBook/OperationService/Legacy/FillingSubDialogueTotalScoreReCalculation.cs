using System;
using System.Threading.Tasks;
using HBLib.AzureFunctions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class FillingSubDialogueTotalScoreReCalculation
    {
        [FunctionName("Filling_Sub_DialogueTotalScoreReCalculation")]
        public static async Task RunAsync(
            Calculations calculations,
            string mySbMsg,
            ExecutionContext dir,
            ILogger log)
        {
            dynamic msgJs = JsonConvert.DeserializeObject(mySbMsg);
            string dialogueId;
            try
            {
                dialogueId = msgJs["DialogueId"];
            }
            catch
            {
                log.LogError($"Failed to read message {mySbMsg}");
                throw;
            }

            try
            {
                calculations.RewriteSatisfactionScore(dialogueId);
            }
            catch (Exception e)
            {
                log.LogError("Exception occured {}", e);
                throw;
            }
        }
    }
}