using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
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

            var totalScore = 35 + CalculateVisual(visual) + CalculateAudio(audio) + CalculateText(speech);

            var ACot11 = 2.5*(Math.PI/2-Math.Atan((totalScore-10)*0.25)) + 1.05;
            var ACot12 = 0.06*(Math.PI/2-Math.Atan((totalScore-80)*0.1)) + 0.75;
            var ATan13 = 0.3*Math.Atan((totalScore-2.5)*1) + 0.465;
            var ACot21 = 0.245*(Math.PI/2-Math.Atan((totalScore-34)*0.7)) + 1.1;
            var ATan22 = 0.12*Math.Atan((totalScore-21)*0.3) + 0.8;

            totalScore = Convert.ToInt16(totalScore * (ACot11 * ACot12 * ATan13 * ACot21 * ATan22));
            if (totalScore > 99) return 99;
            if (totalScore < 35) return 35;
            return Convert.ToInt32(totalScore);
        }

        public double MeetingExpectationsByClientCalculate(Dialogue dialogue)
        {
            try
            {
                var campaignContentIds = _context.SlideShowSessions
                        .Where(p => p.BegTime >= dialogue.BegTime
                                && p.BegTime <= dialogue.EndTime
                                && p.ApplicationUserId == dialogue.ApplicationUserId
                                && p.IsPoll)
                        .Select(p => p.CampaignContentId).ToList();

                Func<string, double> intParse = (string answer) =>
                {
                    switch (answer)
                    {
                        case "EMOTION_ANGRY":
                            return 0;
                        case "EMOTION_BAD":
                            return 2.5;
                        case "EMOTION_NEUTRAL":
                            return 5;
                        case "EMOTION_GOOD":
                            return 7.5;
                        case "EMOTION_EXCELLENT":
                            return 10;
                        default:
                            {
                                Int32.TryParse(answer, out int res);
                                return res != 0? Convert.ToDouble(res): -1;
                            }
                    }

                };
                var pollAnswersAvg = _context.CampaignContentAnswers
                      .Where(x => campaignContentIds.Contains(x.CampaignContentId)
                          && x.Time >= dialogue.BegTime
                          && x.Time <= dialogue.EndTime
                          && x.ApplicationUserId == dialogue.ApplicationUserId).ToList()
                      .Select(x => intParse(x.Answer))
                      .Where(res => res >= 0)
                      .Average() * 10;
                return pollAnswersAvg > 100 ? 100 : pollAnswersAvg;
            }
            catch
            {
                return 0;
            }
        }

        public int CalculateVisual(DialogueVisual visual)
        {
            if (visual == null) return 0;
            return  Convert.ToInt32(100 * (visual.HappinessShare + visual.SurpriseShare) -
                                                    60 * (visual.FearShare + visual.DisgustShare + visual.SadnessShare +
                                                    visual.ContemptShare) +  0.5 * (visual.AttentionShare  - 10));
        }

        public int CalculateAudio(DialogueAudio audio)
        {
            if (audio == null) return 0;
            return Convert.ToInt32((audio.PositiveTone * 50 - audio.NegativeTone * 10));
        }

        public int CalculateText(DialogueSpeech speech)
        {
            if (speech == null) return 0;
            return Convert.ToInt32( 0.3 * (speech.PositiveShare / 4 - 12));
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
            var satisfactionScore = 
                    _context.DialogueClientSatisfactions.FirstOrDefault(p => p.DialogueId.ToString() == dialogueId);

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