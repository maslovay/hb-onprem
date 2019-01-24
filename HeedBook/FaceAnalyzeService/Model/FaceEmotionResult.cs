using System;

namespace FaceAnalyzeService.Model
{
    public class FaceEmotionResult
    {
        public Single Anger { get; set; }

        public Single Contempt { get; set; }

        public Single Disgust { get; set; }

        public Single Fear { get; set; }

        public Single Happiness { get; set; }

        public Single Neutral { get; set; }

        public Single Sadness { get; set; }

        public Single Surprise { get; set; }
    }
}