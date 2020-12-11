namespace ApiPerformance.Models
{
    public class ResponceReportModel
    {
        public string Name;
        public double Duration;
        public int NumberOf200Responce;
        public int NumberOfOtherResponce;
        public override string ToString()
        {
            return $"200: " + NumberOf200Responce.ToString() + "; Others: " + NumberOfOtherResponce.ToString();
        }
    }
}