using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using HBData.Models;
using FillingSatisfactionService.Models;

namespace FillingSatisfactionService.Utils.ScoreCalculations
{
    public class SpeechCalculations
    {
        private readonly LinearRegressionWeightModel _configWeight;
        public SpeechCalculations(LinearRegressionWeightModel configWeight)
        {
            _configWeight = configWeight;
        }

        public int CalculateSpeechScore(DialogueSpeech speech)
        {
            if (speech == null) return 0;
            var speechScore = _configWeight.PositiveShareWeight * speech.PositiveShare +
                _configWeight.SpeechSpeedWeight * speech.SpeechSpeed +
                _configWeight.SilenceShareWeight * speech.SilenceShare;
            System.Console.WriteLine($"Result of speech score {speechScore}");
            return Convert.ToInt32(speechScore);
        }

    }
}