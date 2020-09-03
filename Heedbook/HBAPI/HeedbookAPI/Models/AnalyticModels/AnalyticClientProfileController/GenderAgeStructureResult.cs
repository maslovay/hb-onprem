namespace UserOperations.Models.Get.AnalyticClientProfileController
{
    public class GenderAgeStructureResult
    {
        public string Age { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public double? MaleAverageAge { get; set; }
        public double? FemaleAverageAge { get; set; }
    }
}