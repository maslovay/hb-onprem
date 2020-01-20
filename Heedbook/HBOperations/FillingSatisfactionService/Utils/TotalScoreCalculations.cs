using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using FillingSatisfactionService.Models;
using FillingSatisfactionService.Utils.ScoreCalculations;
using Newtonsoft.Json;

namespace FillingSatisfactionService.Utils
{
    public class TotalScoreCalculations
    {
        private readonly AudioCalculations _audioCalculations;
        private readonly VisualCalculations _visualCalculations;
        private readonly SpeechCalculations _speechCalculations;
        private readonly ClientCalculations _clientCalculations;
        private readonly LinearRegressionWeightModel _linearRegressionWeightModel;


        public TotalScoreCalculations(LinearRegressionWeightModel configWeight,
            AudioCalculations audioCalculations,
            SpeechCalculations speechCalculations,
            VisualCalculations visualCalculations,
            ClientCalculations clientCalculations,
            LinearRegressionWeightModel linearRegressionWeightModel
            )
        {
            _audioCalculations = audioCalculations;
            _speechCalculations = speechCalculations;
            _visualCalculations = visualCalculations;
            _clientCalculations = clientCalculations;
            _linearRegressionWeightModel = linearRegressionWeightModel;
        }

        public int? TotalDialogueScoreCalculate(Dialogue dialogue)
        {
            try
            {
                var totalScore = Convert.ToInt32(_linearRegressionWeightModel.Intersection) +
                    _audioCalculations.CalculateAudioScore(dialogue.DialogueAudio.FirstOrDefault()) +
                    _visualCalculations.CalculateVisualScore(dialogue.DialogueVisual.FirstOrDefault()) +
                    _speechCalculations.CalculateSpeechScore(dialogue.DialogueSpeech.FirstOrDefault());
                if (totalScore > 99) return 99;
                if (totalScore < 35) return 35;
                System.Console.WriteLine($"Total score is {totalScore}");
                return totalScore;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception occured while calculating total score {e}");
                return null;
            }
        }

        public int? BorderDialogueScoreCalculate(List<DialogueFrame> frames, List<DialogueInterval> intervals, DialogueSpeech speech)
        {
            try
            {
                var boarderScore = Convert.ToInt32(_linearRegressionWeightModel.Intersection) + 
                    _audioCalculations.CalculateAudioScore(intervals) +
                    _visualCalculations.CalculateVisualScore(frames) +
                    _speechCalculations.CalculateSpeechScore(speech);
                if (boarderScore > 99) return 99;
                if (boarderScore < 35) return 35;
                System.Console.WriteLine($"Boarder score is {boarderScore}");
                return boarderScore;
            }
            catch (Exception e)
            {
                 System.Console.WriteLine($"Exception occured while calculating boarder score {e}");
                return null;
            }
        }

        public int? ClientDialogueScoreCalculate(Dialogue dialogue, List<CampaignContentAnswer> pollAnswer)
        {
            try
            {
                if (pollAnswer.Any())
                { 
                    var clientScore = _clientCalculations.CalculateClientScore(dialogue, pollAnswer);
                    return clientScore;
                }
                else 
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception occured while calculating client total score {e}");
                return null;
            }
        }
    }
}