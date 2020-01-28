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
            var totalScore = (dialogueSatisfaction.MeetingExpectationsByClient ?? 0) * _config.ClientWeight +
                (dialogueSatisfaction.MeetingExpectationsByEmpoyee ?? 0) * _config.EmployeeWeight +
                (dialogueSatisfaction.MeetingExpectationsByNN ?? 0) * _config.NnWeight +
                (dialogueSatisfaction.MeetingExpectationsByTeacher ?? 0) * _config.TeacherWeight;
            System.Console.WriteLine($"Total score is {totalScore}");
            System.Console.WriteLine((dialogueSatisfaction.MeetingExpectationsByClient ?? 0) * _config.ClientWeight);
            System.Console.WriteLine((dialogueSatisfaction.MeetingExpectationsByEmpoyee ?? 0) * _config.EmployeeWeight);
            System.Console.WriteLine((dialogueSatisfaction.MeetingExpectationsByNN ?? 0) * _config.NnWeight);
            System.Console.WriteLine(_config.NnWeight);
            System.Console.WriteLine(dialogueSatisfaction.MeetingExpectationsByEmpoyee ?? 0);
            System.Console.WriteLine(Math.Sign(dialogueSatisfaction.MeetingExpectationsByEmpoyee ?? 0));


            var totalWeight = Math.Sign(dialogueSatisfaction.MeetingExpectationsByClient ?? 0) * _config.ClientWeight +
                Math.Sign(dialogueSatisfaction.MeetingExpectationsByEmpoyee ?? 0) * _config.EmployeeWeight +
                Math.Sign(dialogueSatisfaction.MeetingExpectationsByNN ?? 0) * _config.NnWeight +
                Math.Sign(dialogueSatisfaction.MeetingExpectationsByTeacher ?? 0) * _config.TeacherWeight;

            return (totalWeight == 0) ? null : (Int32?) Math.Max((Convert.ToInt32(totalScore / totalWeight)), 35);
        }

        public int? RecalculateBegTotalScore(DialogueClientSatisfaction dialogueSatisfaction)
        {
            var begScore = (dialogueSatisfaction.BegMoodByEmpoyee ?? 0) * _config.EmployeeWeight +
                (dialogueSatisfaction.BegMoodByNN ?? 0) * _config.NnWeight +
                (dialogueSatisfaction.BegMoodByTeacher ?? 0) * _config.TeacherWeight;
            var begWeight = Math.Sign(dialogueSatisfaction.BegMoodByEmpoyee ?? 0) * _config.EmployeeWeight +
                Math.Sign(dialogueSatisfaction.BegMoodByNN ?? 0) * _config.NnWeight +
                Math.Sign(dialogueSatisfaction.BegMoodByTeacher ?? 0) * _config.TeacherWeight;

            return (begWeight == 0) ? null : (Int32?) Math.Max(Convert.ToInt32(begScore / begWeight), 35);
        }

        public int? RecalculateEndTotalScore(DialogueClientSatisfaction dialogueSatisfaction)
        {
            var endScore = (dialogueSatisfaction.EndMoodByEmpoyee ?? 0) * _config.EmployeeWeight +
                (dialogueSatisfaction.EndMoodByNN ?? 0)* _config.NnWeight +
                (dialogueSatisfaction.EndMoodByTeacher ?? 0) * _config.TeacherWeight;
            var endWeight = Math.Sign(dialogueSatisfaction.EndMoodByEmpoyee ?? 0) * _config.EmployeeWeight +
                Math.Sign(dialogueSatisfaction.EndMoodByNN ?? 0) * _config.NnWeight +
                Math.Sign(dialogueSatisfaction.EndMoodByTeacher ?? 0) * _config.TeacherWeight;

            return (endWeight == 0) ? null : (Int32?) Math.Max(Convert.ToInt32(endScore / endWeight), 35);
        }


    }
}


