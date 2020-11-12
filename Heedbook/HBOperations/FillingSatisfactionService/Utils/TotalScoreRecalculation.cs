using System;
using HBData.Models;
using FillingSatisfactionService.Models;


namespace FillingSatisfactionService.Utils
{
    public class TotalScoreRecalculations
    {
        private readonly WeightCalculationModel _config;

        public TotalScoreRecalculations(WeightCalculationModel config)
        {
            _config = config;
        }

        public int? RecalculateTotalScore(DialogueClientSatisfaction dialogueSatisfaction)
        {
            var totalScore = dialogueSatisfaction.MeetingExpectationsByClient * _config.ClientWeight +
                dialogueSatisfaction.MeetingExpectationsByEmpoyee * _config.EmployeeWeight +
                dialogueSatisfaction.MeetingExpectationsByNN * _config.NnWeight +
                dialogueSatisfaction.MeetingExpectationsByTeacher * _config.TeacherWeight;
            var totalWeight = Math.Sign(dialogueSatisfaction.MeetingExpectationsByClient ?? 0) * _config.ClientWeight +
                Math.Sign(dialogueSatisfaction.MeetingExpectationsByEmpoyee ?? 0) * _config.EmployeeWeight +
                Math.Sign(dialogueSatisfaction.MeetingExpectationsByNN ?? 0) * _config.NnWeight +
                Math.Sign(dialogueSatisfaction.MeetingExpectationsByTeacher ?? 0) * _config.TeacherWeight;

            return (totalScore == 0) ? null : (Int32?) Math.Max(totalScore / totalWeight ?? 0, 35);
        }

        public int? RecalculateBegTotalScore(DialogueClientSatisfaction dialogueSatisfaction)
        {
            var begScore = dialogueSatisfaction.BegMoodByEmpoyee * _config.EmployeeWeight +
                dialogueSatisfaction.BegMoodByNN * _config.NnWeight +
                dialogueSatisfaction.BegMoodByTeacher * _config.TeacherWeight;
            var begWeight = Math.Sign(dialogueSatisfaction.BegMoodByEmpoyee ?? 0) * _config.EmployeeWeight +
                Math.Sign(dialogueSatisfaction.BegMoodByNN ?? 0) * _config.NnWeight +
                Math.Sign(dialogueSatisfaction.BegMoodByTeacher ?? 0) * _config.TeacherWeight;

            return (begScore == 0) ? null : (Int32?) Math.Max(begScore / begWeight ?? 0, 35);
        }

        public int? RecalculateEndTotalScore(DialogueClientSatisfaction dialogueSatisfaction)
        {
            var endScore = dialogueSatisfaction.EndMoodByEmpoyee * _config.EmployeeWeight +
                dialogueSatisfaction.EndMoodByNN * _config.NnWeight +
                dialogueSatisfaction.EndMoodByTeacher * _config.TeacherWeight;
            var endWeight = Math.Sign(dialogueSatisfaction.EndMoodByEmpoyee ?? 0) * _config.EmployeeWeight +
                Math.Sign(dialogueSatisfaction.EndMoodByNN ?? 0) * _config.NnWeight +
                Math.Sign(dialogueSatisfaction.EndMoodByTeacher ?? 0) * _config.TeacherWeight;

            return (endScore == 0) ? null : (Int32?) Math.Max(endScore / endWeight ?? 0, 35);
        }


    }
}


