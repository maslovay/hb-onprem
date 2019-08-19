using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using HBData.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FillingSatisfactionService.Helper
{
    public class Calculations
    {
        private readonly CalculationConfig _config;
        private readonly RecordsContext _context;

        public Calculations(IServiceScopeFactory factory,
            CalculationConfig config)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _config = config;
        }

        public List<DialogueClientSatisfaction> TotalScoreCalculate(List<Dialogue> dialogues)
        {
            var dialoguesIdName = new List<String>();
            foreach (var dialogue in dialogues) dialoguesIdName.Add(dialogue.DialogueId.ToString());

            var visuals = _context.DialogueVisuals.Where(p => dialoguesIdName.Contains(p.DialogueId.ToString()))
                                  .ToList();
            var audios = _context.DialogueAudios.Where(p => dialoguesIdName.Contains(p.DialogueId.ToString())).ToList();
            var speechs = _context.DialogueSpeechs.Where(p => dialoguesIdName.Contains(p.DialogueId.ToString()))
                                  .ToList();
            var satisfactions = _context.DialogueClientSatisfactions
                                        .Where(p => dialoguesIdName.Contains(p.Dialogue.ToString())).ToList();
            foreach (var dialogue in dialogues)
            {
                var dialogueId = dialogue.DialogueId;
                var visual = visuals.Find(p => p.DialogueId == dialogueId);
                var audio = audios.Find(p => p.DialogueId == dialogueId);
                var speech = speechs.Find(p => p.DialogueId == dialogueId);
                var satisfaction = satisfactions.Find(p => p.DialogueId == dialogueId);

                var TotalScore = Math.Round((Decimal) (80 + visual.HappinessShare + visual.SurpriseShare -
                                                       (visual.FearShare + visual.DisgustShare + visual.SadnessShare +
                                                        visual.ContemptShare) +
                                                       (audio.PositiveTone * 0.5 - audio.NegativeTone * 0.3) +
                                                       (visual.AttentionShare / 3 - 27) +
                                                       (speech.PositiveShare / 4 - 18)), 0);

                satisfaction.MeetingExpectationsTotal = (Single) TotalScore;
                if (satisfaction.MeetingExpectationsTotal > 99) satisfaction.MeetingExpectationsTotal = 99;

                if (satisfaction.MeetingExpectationsTotal < 10) satisfaction.MeetingExpectationsTotal = 10;
            }

            return satisfactions;
        }

        public Int32 TotalScoreInsideCalculate(IEnumerable<DialogueFrame> DF, DialogueAudio DA,
            Double? PositiveTextTone)
        {
            var FaceYawMax = _config.FaceYawMax;
            var FaceYawMin = _config.FaceYawMin;
            var TotalScore = 0;
            var rand = new Random();
            try
            {
                var framesSatisfaction =
                    37*((Convert.ToDouble(DF
                        .Where(s => s.HappinessShare != null)
                        .Average(s => s.HappinessShare))
                    + Convert.ToDouble(DF
                        .Where(s => s.SurpriseShare != null)
                        .Average(s => s.SurpriseShare))
                    - 0.1*(Convert.ToDouble(DF
                        .Where(s => s.AngerShare != null)
                        .Average(s => s.AngerShare))
                    + Convert.ToDouble(DF
                        .Where(s => s.FearShare != null)
                        .Average(s => s.FearShare))
                    + Convert.ToDouble(DF
                        .Where(s => s.ContemptShare != null)
                        .Average(s => s.ContemptShare))
                    + Convert.ToDouble(DF
                        .Where(s => s.DisgustShare != null)
                        .Average(s => s.DisgustShare))
                    + Convert.ToDouble(DF
                        .Where(s => s.SadnessShare != null)
                        .Average(s => s.SadnessShare))))                                
                    + 3 * ((DF.Where(s => s.YawShare >= FaceYawMin && s.YawShare <= FaceYawMax).Count()
                        / (DF.Count() + 1))));
                var audioSatisfaction = 24*((DA.PositiveTone == null ? 0 : (double)DA.PositiveTone)
                    - 3/5 * (DA.NegativeTone == null ? 0 : (double)DA.NegativeTone));
                var textSatisfaction = (PositiveTextTone);

                var arcCtgWeight = 0.6*(Math.PI/2-Math.Atan((framesSatisfaction-3)*0.8)) + 0.9;
                var arcTgWeight = 0.6*Math.Atan((framesSatisfaction-2)*0.5) + 1;
                
                TotalScore = Convert.ToInt32(33 
                    + (framesSatisfaction + audioSatisfaction + textSatisfaction)*(arcCtgWeight * arcTgWeight)
                    +rand.Next(-5, 6)+15);
                        

                if (TotalScore > 99) TotalScore = 99;

                if (TotalScore < 35) TotalScore = 35;
            }
            catch
            {
                TotalScore = 0;
            }

            return TotalScore;
        }

        public Int32 BorderMoodCalculate(DialogueFrame dialogueFrame, DialogueInterval dialogueInterval)
        {
            var faceYawMax = _config.FaceYawMax;
            var faceYawMin = _config.FaceYawMin;
            var begScore = 0;
            try
            {
                var yaw = 0;
                if (Math.Abs(Convert.ToDouble(dialogueFrame.YawShare)) >
                    Math.Min(Math.Abs(faceYawMax), Math.Abs(faceYawMin)))
                    yaw = 10;

                begScore = (Int32) Math.Round((Decimal)
                    (80 + 2 * (dialogueFrame.SurpriseShare + dialogueFrame.HappinessShare) -
                     (dialogueFrame.AngerShare + dialogueFrame.ContemptShare + dialogueFrame.DisgustShare +
                      3 * dialogueFrame.SadnessShare + dialogueFrame.FearShare)
                     + (3 * dialogueInterval.HappinessTone - dialogueInterval.SadnessTone - dialogueInterval.AngerTone -
                        dialogueInterval.FearTone) - yaw));
                if (begScore > 99) begScore = 99;

                if (begScore < 10) begScore = 10;
            }
            catch
            {
                begScore = 0;
            }

            return begScore;
        }

        public Int32 BorderMoodCalculateList(List<DialogueFrame> dialogueFrame, List<DialogueInterval> dialogueInterval,
            Int32 averageScore)
        {
            var score = 0;
            var lenght = 0;
            for (var i = 0; i < dialogueFrame.Count(); i++)
                try
                {
                    var result = BorderMoodCalculate(dialogueFrame[i], dialogueInterval[i]);
                    score += result != 0 ? result : 0;
                    lenght += result != 0 ? 1 : 0;
                }
                catch
                {
                }

            score = score != 0 ? score / lenght : averageScore;
            return score;
        }

        public void RewriteSatisfactionScore(String dialogueId)
        {
            var satisfactionScore = new DialogueClientSatisfaction();
            try
            {
                satisfactionScore =
                    _context.DialogueClientSatisfactions.First(p => p.DialogueId.ToString() == dialogueId);
            }
            catch
            {
                satisfactionScore = null;
            }

            if (satisfactionScore != null)
            {
                Double? clientWeight = 0, employeeWeight = 0, teacherWeight = 0, nNWeight = 0;
                Double? employeeBegScore = 0, teacherBegScore = 0, NNBegScore = 0;
                Double? employeeEndScore = 0, teacherEndScore = 0, NNEndScore = 0;
                Double? clientScore = 0, teacherScore = 0, employeeScore = 0, NNScore = 0;
                if (satisfactionScore.MeetingExpectationsByClient != null)
                {
                    clientScore = satisfactionScore.MeetingExpectationsByClient;
                    clientWeight = _config.ClientWeight;
                }

                if (satisfactionScore.MeetingExpectationsByEmpoyee != null)
                {
                    employeeWeight = _config.EmployeeWeight;
                    employeeScore = satisfactionScore.MeetingExpectationsByEmpoyee;
                    if (satisfactionScore.BegMoodByEmpoyee != null)
                        employeeBegScore = satisfactionScore.BegMoodByEmpoyee;

                    if (satisfactionScore.EndMoodByEmpoyee != null)
                        employeeEndScore = satisfactionScore.EndMoodByEmpoyee;
                }

                if (satisfactionScore.MeetingExpectationsByTeacher != null ||
                    satisfactionScore.MeetingExpectationsByTeacher != 0)
                {
                    teacherWeight = _config.TeacherWeight;
                    teacherScore = satisfactionScore.MeetingExpectationsByTeacher;
                    if (satisfactionScore.BegMoodByTeacher != null)
                        teacherBegScore = satisfactionScore.BegMoodByTeacher;

                    if (satisfactionScore.EndMoodByTeacher != null)
                        teacherEndScore = satisfactionScore.EndMoodByTeacher;
                }

                if (satisfactionScore.MeetingExpectationsByNN != null)
                {
                    nNWeight = _config.NnWeight;
                    NNScore = satisfactionScore.MeetingExpectationsByNN;
                    if (satisfactionScore.BegMoodByNN != null) NNBegScore = satisfactionScore.BegMoodByNN;

                    if (satisfactionScore.EndMoodByNN != null) NNEndScore = satisfactionScore.EndMoodByNN;
                }

                var sumWeight = nNWeight + clientWeight + employeeWeight + teacherWeight;
                Double? meetingExpectationsTotal = 0, begMoodTotal = 0, endMoodTotal = 0;
                if (sumWeight != 0)
                    meetingExpectationsTotal =
                        (clientWeight * clientScore + nNWeight * NNScore + employeeWeight * employeeScore +
                         teacherWeight * teacherScore) / sumWeight;

                var sumWeightExceptClient = nNWeight + employeeWeight + teacherWeight;
                if (sumWeightExceptClient != 0)
                {
                    begMoodTotal =
                        (nNWeight * NNBegScore + employeeBegScore * employeeWeight + teacherBegScore * teacherWeight) /
                        sumWeightExceptClient;
                    endMoodTotal =
                        (nNWeight * NNEndScore + employeeEndScore * employeeWeight + teacherEndScore * teacherWeight) /
                        sumWeightExceptClient;
                }

                satisfactionScore.MeetingExpectationsTotal = meetingExpectationsTotal;
                satisfactionScore.BegMoodTotal = begMoodTotal;
                satisfactionScore.EndMoodTotal = endMoodTotal;
                _context.SaveChanges();
            }
        }
    }
}