using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using HBData.Models;

namespace FillingSatisfactionService.Helper
{
    public class Calculations
    {
        public static List<DialogueClientSatisfaction> TotalScoreCalculate(List<Dialogue> dialogues) {
            using (var context = new RecordsContext()) {
                var dialoguesIdName = new List<string>();
                foreach (var dialogue in dialogues) {
                    dialoguesIdName.Add(dialogue.DialogueId.ToString());
                }
                var visuals = context.DialogueVisuals.Where(p => dialoguesIdName.Contains(p.DialogueId.ToString())).ToList();
                var audios = context.DialogueAudios.Where(p => dialoguesIdName.Contains(p.DialogueId.ToString())).ToList();
                var speechs = context.DialogueSpeechs.Where(p => dialoguesIdName.Contains(p.DialogueId.ToString())).ToList();
                var satisfactions = context.DialogueClientSatisfactions.Where(p => dialoguesIdName.Contains(p.Dialogue.ToString())).ToList();
                foreach (var dialogue in dialogues) {
                    var dialogueId = dialogue.DialogueId;
                    var visual = visuals.Find(p => p.DialogueId == dialogueId);
                    var audio = audios.Find(p => p.DialogueId == dialogueId);
                    var speech = speechs.Find(p => p.DialogueId == dialogueId);
                    var satisfaction = satisfactions.Find(p => p.DialogueId == dialogueId);

                    var TotalScore = Math.Round((decimal)(80 + (visual.HappinessShare) + (visual.SurpriseShare) - (visual.FearShare + visual.DisgustShare + visual.SadnessShare + visual.ContemptShare) +
                        (audio.PositiveTone * 0.5 - audio.NegativeTone * 0.3) + ((visual.AttentionShare / 3) - 27) + (speech.PositiveShare / 4 - 18)), 0);

                    satisfaction.MeetingExpectationsTotal = (float)TotalScore;
                    if (satisfaction.MeetingExpectationsTotal > 99) {
                        satisfaction.MeetingExpectationsTotal = 99;
                    }
                    if (satisfaction.MeetingExpectationsTotal < 10) {
                        satisfaction.MeetingExpectationsTotal = 10;
                    }
                }
                return satisfactions;
            }
        }

        public static int TotalScoreInsideCalculate(List<DialogueFrame> DF, DialogueAudio DA, double? PositiveTextTone) {
            var FaceYawMax = Convert.ToDouble(EnvVar.Get("FaceYawMax"));
            var FaceYawMin = Convert.ToDouble(EnvVar.Get("FaceYawMin"));
            int TotalScore = 0;
            try {
                TotalScore = Convert.ToInt32(80 +
                    100 * (Convert.ToDouble(DF.Where(p => p.HappinessShare != null).Average(p => p.HappinessShare)) 
                    + Convert.ToDouble(DF.Where(p => p.SurpriseShare != null).Average(p => p.SurpriseShare))
                    - Convert.ToDouble(DF.Where(p => p.AngerShare != null).Average(p => p.AngerShare))
                    - Convert.ToDouble(DF.Where(p => p.FearShare != null).Average(p => p.FearShare))
                    - Convert.ToDouble(DF.Where(p => p.ContemptShare != null).Average(p => p.ContemptShare))
                    - Convert.ToDouble(DF.Where(p => p.DisgustShare != null).Average(p => p.DisgustShare))
                    - Convert.ToDouble(DF.Where(p => p.SadnessShare != null).Average(p => p.SadnessShare)))
                    + Convert.ToDouble((DA.PositiveTone * 0.5 - DA.NegativeTone * 0.3))
                    + ((DF.Where(p => p.YawShare >= FaceYawMin && p.YawShare <= FaceYawMax).Count() * 100) / (DF.Count() + 1) / 3 - 27) 
                    + Convert.ToDouble(PositiveTextTone / 4 - 18));

                if (TotalScore > 99) {
                    TotalScore = 99;
                }
                if (TotalScore < 10) {
                    TotalScore = 10;
                }
            } catch {
                TotalScore = 0;
            }
            return TotalScore;
        }

        public static int BorderMoodCalculate(DialogueFrame dialogueFrame, DialogueInterval dialogueInterval) {
            var faceYawMax = Convert.ToDouble(EnvVar.Get("FaceYawMax"));
            var faceYawMin = Convert.ToDouble(EnvVar.Get("FaceYawMin"));
            var begScore = 0;
            try {
                var yaw = 0;
                if (Math.Abs(Convert.ToDouble(dialogueFrame.YawShare)) > Math.Min(Math.Abs(faceYawMax), Math.Abs(faceYawMin))) {
                    yaw = 10;
                }
                begScore = (int)Math.Round((decimal)
                    (80 + 2 * (dialogueFrame.SurpriseShare + dialogueFrame.HappinessShare) - (dialogueFrame.AngerShare + dialogueFrame.ContemptShare + dialogueFrame.DisgustShare + 3 * dialogueFrame.SadnessShare + dialogueFrame.FearShare)
                    + (3 * dialogueInterval.HappinessTone - dialogueInterval.SadnessTone - dialogueInterval.AngerTone - dialogueInterval.FearTone) - yaw));
                if (begScore > 99) {
                    begScore = 99;
                }
                if (begScore < 10) {
                    begScore = 10;
                }
            } catch {
                begScore = 0;
            }
            return begScore;
        }

        public static int BorderMoodCalculateList(List<DialogueFrame> dialogueFrame, List<DialogueInterval> dialogueInterval, int averageScore)
        {
            var score = 0;
            var lenght = 0;
            for (int i = 0; i < dialogueFrame.Count(); i++)
            {
                try
                {
                    var result = BorderMoodCalculate(dialogueFrame[i], dialogueInterval[i]);
                    score += result != 0 ? result : 0;
                    lenght += result != 0 ? 1 : 0;
                }
                catch
                {

                }
            }
            score = score != 0 ? score / lenght : averageScore;
            return score;
        }

        public static void RewriteSatisfactionScore(string dialogueId)
        {
            var context = new RecordsContext();
            var satisfactionScore = new DialogueClientSatisfaction();
            try
            {
                satisfactionScore = context.DialogueClientSatisfactions.First(p => p.DialogueId.ToString() == dialogueId);
            }
            catch
            {
                satisfactionScore = null;
            }
            if (satisfactionScore != null)
            {
                double? clientWeight = 0, employeeWeight = 0, teacherWeight = 0, nNWeight = 0;
                double? employeeBegScore = 0, teacherBegScore = 0, NNBegScore = 0;
                double? employeeEndScore = 0, teacherEndScore = 0, NNEndScore = 0;
                double? clientScore = 0, teacherScore = 0, employeeScore = 0, NNScore = 0;
                if (satisfactionScore.MeetingExpectationsByClient != null)
                {
                    clientScore = satisfactionScore.MeetingExpectationsByClient;
                    clientWeight = Convert.ToDouble(EnvVar.Get("ClientWeight"));
                }
                if (satisfactionScore.MeetingExpectationsByEmpoyee != null)
                {
                    employeeWeight = Convert.ToDouble(EnvVar.Get("EmployeeWeight"));
                    employeeScore = satisfactionScore.MeetingExpectationsByEmpoyee;
                    if (satisfactionScore.BegMoodByEmpoyee != null)
                    {
                        employeeBegScore = satisfactionScore.BegMoodByEmpoyee;
                    }
                    if (satisfactionScore.EndMoodByEmpoyee != null)
                    {
                        employeeEndScore = satisfactionScore.EndMoodByEmpoyee;
                    }
                }
                if (satisfactionScore.MeetingExpectationsByTeacher != null || satisfactionScore.MeetingExpectationsByTeacher != 0)
                {
                    teacherWeight = Convert.ToDouble(EnvVar.Get("TeacherWeight"));
                    teacherScore = satisfactionScore.MeetingExpectationsByTeacher;
                    if (satisfactionScore.BegMoodByTeacher != null)
                    {
                        teacherBegScore = satisfactionScore.BegMoodByTeacher;
                    }
                    if (satisfactionScore.EndMoodByTeacher != null)
                    {
                        teacherEndScore = satisfactionScore.EndMoodByTeacher;
                    }
                }
                if (satisfactionScore.MeetingExpectationsByNN != null)
                {
                    nNWeight = Convert.ToDouble(EnvVar.Get("NNWeight"));
                    NNScore = satisfactionScore.MeetingExpectationsByNN;
                    if (satisfactionScore.BegMoodByNN != null)
                    {
                        NNBegScore = satisfactionScore.BegMoodByNN;
                    }
                    if (satisfactionScore.EndMoodByNN != null)
                    {
                        NNEndScore = satisfactionScore.EndMoodByNN;
                    }
                }
                var sumWeight = nNWeight + clientWeight + employeeWeight + teacherWeight;
                double? meetingExpectationsTotal = 0, begMoodTotal = 0, endMoodTotal = 0;
                if (sumWeight != 0)
                {
                    meetingExpectationsTotal = (clientWeight * clientScore + nNWeight * NNScore + employeeWeight * employeeScore + teacherWeight * teacherScore) / sumWeight;
                }
                var sumWeightExceptClient = nNWeight + employeeWeight + teacherWeight;
                if (sumWeightExceptClient != 0)
                {
                    begMoodTotal = (nNWeight * NNBegScore + employeeBegScore * employeeWeight + teacherBegScore * teacherWeight) / sumWeightExceptClient;
                    endMoodTotal = (nNWeight * NNEndScore + employeeEndScore * employeeWeight + teacherEndScore * teacherWeight) / sumWeightExceptClient;
                }
                satisfactionScore.MeetingExpectationsTotal = meetingExpectationsTotal;
                satisfactionScore.BegMoodTotal = begMoodTotal;
                satisfactionScore.EndMoodTotal = endMoodTotal;
                context.SaveChanges();
            }
}
    }
}