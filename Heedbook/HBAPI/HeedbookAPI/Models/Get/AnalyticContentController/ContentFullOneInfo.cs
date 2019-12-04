namespace UserOperations.Models.Get.AnalyticContentController
{
    public class ContentFullOneInfo
    {
        public string Content { get; set; }
        public string ContentName { get; set; }
        public int AmountViews { get; set; }
        public EmotionAttention EmotionAttention { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public double? Age { get; set; }
    }
}