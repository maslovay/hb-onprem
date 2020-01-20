using System;

namespace FillingSatisfactionService.Models
{
    public class LinearRegressionWeightModel
    {
        public Double PositiveToneWeight { get; set; }
        public Double NegativeToneWeight { get; set; }
        public Double NeutralityToneWeight { get; set; }
        public Double AttentionShareWeight { get; set; }
        public Double HappinessShareWeight { get; set; }
        public Double NeutralShareWeight { get; set; }
        public Double SurpriseShareWeight { get; set; }
        public Double SadnessShareWeight { get; set; }
        public Double AngerShareWeight { get; set; }
        public Double DisgustShareWeight { get; set; }
        public Double ContemptShareWeight { get; set; }
        public Double FearShareWeight { get; set; }
        public Double PositiveShareWeight { get; set; }
        public Double SpeechSpeedWeight { get; set; }
        public Double SilenceShareWeight { get; set; }
        public Double Intersection {get; set;}

    }
}