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

        public int TotalScoreCalculate(Dialogue dialogue)
        {
            var visuals = _context.DialogueVisuals.Where(p => p.DialogueId == dialogue.DialogueId)
                                  .ToList();
            var audios = _context.DialogueAudios.Where(p => p.DialogueId == dialogue.DialogueId).ToList();
            var speechs = _context.DialogueSpeechs.Where(p => p.DialogueId == dialogue.DialogueId).ToList();

            var dialogueId = dialogue.DialogueId;
            var visual = visuals.Find(p => p.DialogueId == dialogueId);
            var audio = audios.Find(p => p.DialogueId == dialogueId);
            var speech = speechs.Find(p => p.DialogueId == dialogueId);

            var totalScore = Math.Round((Decimal) (60 + 100 * (visual.HappinessShare + visual.SurpriseShare) -
                                                    60 * (visual.FearShare + visual.DisgustShare + visual.SadnessShare +
                                                    visual.ContemptShare) +
                                                    (audio.PositiveTone * 50 - audio.NegativeTone * 10) +
                                                    0.5 * (visual.AttentionShare  - 10) +
                                                    0.3 * (speech.PositiveShare / 4 - 12)), 0);


            if (totalScore > 99) return 99;

            if (totalScore < 10) return 10;
            

            return Convert.ToInt32(totalScore);
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
            }
            catch
            {
                TotalScore = 0;
            }
            
            if (TotalScore > 99) TotalScore = 99;

            if (TotalScore < 35) TotalScore = 35;

            return TotalScore;
        }

        public Int32 BorderMoodCalculate(DialogueFrame dialogueFrame, DialogueInterval dialogueInterval)
        {
            var faceYawMax = _config.FaceYawMax;
            var faceYawMin = _config.FaceYawMin;
            var begScore = 0;
            try
            {
                var frameSatisfaction = 30 * (dialogueFrame.HappinessShare + dialogueFrame.SurpriseShare 
                    - 0.1 * (dialogueFrame.AngerShare 
                        + dialogueFrame.FearShare 
                        + dialogueFrame.ContemptShare 
                        + dialogueFrame.DisgustShare 
                        + dialogueFrame.SadnessShare)
                    + ((dialogueFrame.YawShare >= faceYawMin && dialogueFrame.YawShare <= faceYawMax) ? 3 : 0));

                double audioSatisfaction = 0;
                if(dialogueInterval!=null)
                {
                    audioSatisfaction = 24*((3 * dialogueInterval.HappinessTone == null ? 0 : (double)dialogueInterval.HappinessTone
                    - dialogueInterval.SadnessTone ==null ? 0 : (double)dialogueInterval.SadnessTone
                    - dialogueInterval.AngerTone == null ? 0 : (double)dialogueInterval.AngerTone
                    - dialogueInterval.FearTone == null ? 0 : (double)dialogueInterval.FearTone));
                }
                

                var arcCtgWeight = 0.6*(Math.PI/2-Math.Atan(((double)frameSatisfaction-3)*0.8)) + 0.9;
                var arcTgWeight = 0.6*Math.Atan(((double)frameSatisfaction-2)*0.5) + 0.3;

                var rand = new Random();
                begScore = Convert.ToInt32(33 
                    + (frameSatisfaction + audioSatisfaction)*(arcCtgWeight * arcTgWeight)
                    +rand.Next(-3, 3)+5);
            }
            catch
            {
                begScore = 0;
            }

            if (begScore > 99) begScore = 99;

            if (begScore < 35) begScore = 35;

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