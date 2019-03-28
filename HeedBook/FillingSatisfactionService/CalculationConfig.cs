using System;

namespace FillingSatisfactionService
{
    public class CalculationConfig
    {
        public Double FaceYawMax { get; set; }
        public Double FaceYawMin { get; set; }
        public Double ClientWeight { get; set; }
        public Double EmployeeWeight { get; set; }
        public Double TeacherWeight { get; set; }
        public Double NNWeightD { get; set; }
    }
}