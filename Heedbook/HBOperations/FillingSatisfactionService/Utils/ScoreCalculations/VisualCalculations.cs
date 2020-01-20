using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using HBData.Models;
using FillingSatisfactionService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FillingSatisfactionService.Utils.ScoreCalculations
{
    public class VisualCalculations
    {
        private readonly LinearRegressionWeightModel _configWeight;
        public VisualCalculations(LinearRegressionWeightModel configWeight)
        {
            _configWeight = configWeight;
        }

       public int CalculateVisualScore(DialogueVisual visual)
        {
            if (visual == null) return 0;
            var visualScore = _configWeight.HappinessShareWeight * visual.HappinessShare +
                _configWeight.NeutralShareWeight * visual.NeutralShare + 
                _configWeight.SurpriseShareWeight * visual.SurpriseShare +
                _configWeight.SadnessShareWeight * visual.SadnessShare +
                _configWeight.AngerShareWeight * visual.AngerShare + 
                _configWeight.DisgustShareWeight * visual.DisgustShare + 
                _configWeight.ContemptShareWeight * visual.ContemptShare +
                _configWeight.FearShareWeight * visual.FearShare +
                _configWeight.AttentionShareWeight * visual.AttentionShare;
            return  Convert.ToInt32(visualScore);
        }

        public int CalculateVisualScore(List<DialogueFrame> visual)
        {
            if (visual == null) return 0;
            var visualScore2 = _configWeight.HappinessShareWeight * (visual.Average(p => p.HappinessShare) ?? 0) +
                _configWeight.NeutralShareWeight * (visual.Average(p => p.NeutralShare) ?? 0) + 
                _configWeight.SurpriseShareWeight * (visual.Average(p => p.SurpriseShare) ?? 0) +
                _configWeight.SadnessShareWeight * (visual.Average(p => p.SadnessShare) ?? 0) +
                _configWeight.AngerShareWeight * (visual.Average(p => p.AngerShare) ?? 0) + 
                _configWeight.DisgustShareWeight * (visual.Average(p => p.DisgustShare) ?? 0) + 
                _configWeight.ContemptShareWeight * (visual.Average(p => p.ContemptShare) ?? 0) +
                _configWeight.FearShareWeight * (visual.Average(p => p.FearShare) ?? 0) +
                _configWeight.AttentionShareWeight * (10 * (10 - Math.Min(visual.Average(p => p.YawShare) ?? 0, 10) / 1.4));
            return  Convert.ToInt32(visualScore2);
        }
    }
}