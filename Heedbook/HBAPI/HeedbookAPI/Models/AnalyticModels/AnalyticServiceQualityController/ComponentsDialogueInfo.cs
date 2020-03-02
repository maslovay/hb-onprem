using System;

namespace UserOperations.Models.Get.AnalyticServiceQualityController
{
    public class ComponentsDialogueInfo
    {
        public Guid DialogueId;
        public double? PositiveTone;
        public double? NegativeTone;
        public double? NeutralityTone;
        public double? EmotivityShare;
        public double? HappinessShare;
        public double? NeutralShare;
        public double? SurpriseShare;
        public double? SadnessShare;
        public double? AngerShare;
        public double? DisgustShare;
        public double? ContemptShare;
        public double? FearShare;
        public double? AttentionShare;
    //  public double? Cross;
    //  public double? Necessary;
        public int Loyalty;
    //  public double? Alert;
    //  public double? Fillers;
    //  public double? Risk;
    }
}