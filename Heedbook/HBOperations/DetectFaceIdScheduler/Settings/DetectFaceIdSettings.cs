namespace DetectFaceIdScheduler.Settings
{
    public class DetectFaceIdSettings
    {
        public int PeriodFrames { get; set; }
        public int PeriodTime {get; set;}
        public double Threshold {get;set;}
        public int TimeGapRequest {get;set;}
        public int MinWidth {get;set;}
        public int MinHeight{get;set;}
    }
}