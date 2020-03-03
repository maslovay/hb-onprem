using System;

namespace FillingSatisfactionService.Models
{
    public class WeightCalculationModel
    {
        public Double FaceYawMax { get; set; }
        public Double FaceYawMin { get; set; }
        public Double ClientWeight { get; set; }
        public Double EmployeeWeight { get; set; }
        public Double TeacherWeight { get; set; }
        public Double NnWeight { get; set; }
    }
}