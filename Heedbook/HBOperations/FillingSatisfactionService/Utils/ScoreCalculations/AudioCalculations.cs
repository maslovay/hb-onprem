using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using FillingSatisfactionService.Models;

namespace FillingSatisfactionService.Utils.ScoreCalculations
{
    public class AudioCalculations
    {
        private readonly LinearRegressionWeightModel _configWeight;
        public AudioCalculations(LinearRegressionWeightModel configWeight)
        {
            _configWeight = configWeight;
        }

        public int CalculateAudioScore(DialogueAudio audio)
        {
            if (audio == null) return 0;
            var audioScore = _configWeight.PositiveToneWeight * audio.PositiveTone +
                _configWeight.NegativeToneWeight * audio.NegativeTone +
                _configWeight.NeutralityToneWeight * audio.NeutralityTone;
            System.Console.WriteLine($"Result of audio score {audioScore}");
            return Convert.ToInt32(audioScore);
        }

        public int CalculateAudioScore(List<DialogueInterval> audio)
        {
            if (!audio.Any()) return 0;
            var audioScore = _configWeight.PositiveToneWeight * audio.Average(p => p.HappinessTone) ?? 0.0 +
                _configWeight.NegativeToneWeight * audio.Average(p => p.FearTone + p.SadnessTone + p.AngerTone) ?? 0.0 +
                _configWeight.NeutralityToneWeight * audio.Average(p => p.NeutralityTone) ?? 0;
            System.Console.WriteLine($"Result of audio score {audioScore}");
            return Convert.ToInt32(audioScore);
        }
    }
}