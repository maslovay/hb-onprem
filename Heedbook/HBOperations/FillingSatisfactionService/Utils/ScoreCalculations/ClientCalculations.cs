using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using FillingSatisfactionService.Models;

namespace FillingSatisfactionService.Utils.ScoreCalculations
{
    public class ClientCalculations
    {
        private readonly LinearRegressionWeightModel _configWeight;
        public ClientCalculations(LinearRegressionWeightModel configWeight)
        {
            _configWeight = configWeight;
        }

        public int CalculateClientScore(Dialogue dialogue, List<CampaignContentAnswer> pollAnswer)
        {
            Func<string, double> intParse = (string answer) =>
            {
                switch (answer)
                {
                    case "EMOTION_ANGRY":
                        return 0;
                    case "EMOTION_BAD":
                        return 2.5;
                    case "EMOTION_NEUTRAL":
                        return 5;
                    case "EMOTION_GOOD":
                        return 7.5;
                    case "EMOTION_EXCELLENT":
                        return 10;
                    default:
                    {
                        Int32.TryParse(answer, out int res);
                        return res != 0? Convert.ToDouble(res): -1;
                    }
                }
            };
            // _context.CampaignContentAnswers
                    // .Where(x => x.Time >= dialogue.BegTime
                        // && x.Time <= dialogue.EndTime
                        // && x.ApplicationUserId == dialogue.ApplicationUserId).ToList()
            var pollAnswersAvg = 10 * pollAnswer.Select(x => intParse(x.Answer))
                    .Where(res => res >= 0)
                    .Average();
            if (pollAnswersAvg > 100) return 100;
            if (pollAnswersAvg < 0) return 0;
            System.Console.WriteLine($"Result of poll score {pollAnswersAvg}");
            return Convert.ToInt32(pollAnswersAvg);
            
        }
    }
}