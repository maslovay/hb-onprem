using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Models;
using HBLib.Utils;

namespace HBLib.AzureFunctions
{
    public class Calculations
    {
        private readonly IGenericRepository _repository;

        public Calculations(IGenericRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<DialogueClientSatisfaction>> TotalScoreCalculate(List<Dialogue> dialogues)
        {
            var dialoguesIdName = new List<String>();
            foreach (var dialogue in dialogues) dialoguesIdName.Add(dialogue.DialogueId.ToString());

            var visualsTask = _repository
               .FindByConditionAsyncToDictionary<DialogueVisual, Guid?, DialogueVisual>(p =>
                        dialoguesIdName
                           .Contains(p.DialogueId.ToString()),
                    item => item.DialogueId,
                    item => item
                );
            var audiosTask = _repository
               .FindByConditionAsyncToDictionary<DialogueAudio, Guid?, DialogueAudio>(p =>
                        dialoguesIdName
                           .Contains(p.DialogueId.ToString()),
                    item => item.DialogueId,
                    item => item
                );
            var speechesTask = _repository
               .FindByConditionAsyncToDictionary<DialogueSpeech, Guid?, DialogueSpeech>(p =>
                        dialoguesIdName
                           .Contains(p.DialogueId.ToString()),
                    item => item.DialogueId,
                    item => item);
            var satisfactionsTask = _repository
               .FindByConditionAsyncToDictionary<DialogueClientSatisfaction, Guid?, DialogueClientSatisfaction>(p =>
                        dialoguesIdName
                           .Contains(p.Dialogue.ToString()),
                    item => item.DialogueId,
                    item => item
                );
            await Task.WhenAll(visualsTask, audiosTask, speechesTask, satisfactionsTask);

            var (visuals, audios, speeches, satisfactions) =
            (
                visualsTask.Result,
                audiosTask.Result,
                speechesTask.Result,
                satisfactionsTask.Result
            );
            foreach (var dialogue in dialogues)
            {
                var dialogueId = dialogue.DialogueId;
                var visual = visuals[dialogueId];
                var audio = audios[dialogueId];
                var speech = speeches[dialogueId];
                var satisfaction = satisfactions[dialogueId];

                if (visual.FearShare != null)
                {
                    var totalScore = (Single) Math.Round((Decimal) (80
                                                                    + visual.HappinessShare.Value +
                                                                    visual.SurpriseShare.Value
                                                                    - (visual.FearShare.Value + visual.DisgustShare
                                                                                                      .Value
                                                                                              + visual.SadnessShare
                                                                                                      .Value +
                                                                                              visual.ContemptShare.Value
                                                                    )
                                                                    + (audio.PositiveTone.Value * 0.5 -
                                                                       audio.NegativeTone.Value * 0.3)
                                                                    + (visual.AttentionShare.Value / 3 - 27)
                                                                    + (speech.PositiveShare.Value / 4 - 18)),
                        0);

                    satisfaction.MeetingExpectationsTotal = totalScore;
                }

                if (satisfaction.MeetingExpectationsTotal > 99) satisfaction.MeetingExpectationsTotal = 99;

                if (satisfaction.MeetingExpectationsTotal < 10) satisfaction.MeetingExpectationsTotal = 10;
            }

            return satisfactions.Values.ToList();
        }

        public static Int32 TotalScoreInsideCalculate(List<DialogueFrame> dialogueFrames, DialogueAudio dialogueAudio,
            Double? PositiveTextTone)
        {
            var faceYawMax = Convert.ToDouble(EnvVar.Get("FaceYawMax"));
            var faceYawMin = Convert.ToDouble(EnvVar.Get("FaceYawMin"));
            var totalScore = 0;
            try
            {
                totalScore = Convert.ToInt32(80 +
                                             100 * (Convert.ToDouble(dialogueFrames.Where(p => p.HappinessShare != null)
                                                                                   .Average(p => p.HappinessShare))
                                                    + Convert.ToDouble(dialogueFrames
                                                                      .Where(p => p.SurpriseShare != null)
                                                                      .Average(p => p.SurpriseShare))
                                                    - Convert.ToDouble(dialogueFrames.Where(p => p.AngerShare != null)
                                                                                     .Average(p => p.AngerShare))
                                                    - Convert.ToDouble(dialogueFrames.Where(p => p.FearShare != null)
                                                                                     .Average(p => p.FearShare))
                                                    - Convert.ToDouble(dialogueFrames
                                                                      .Where(p => p.ContemptShare != null)
                                                                      .Average(p => p.ContemptShare))
                                                    - Convert.ToDouble(dialogueFrames.Where(p => p.DisgustShare != null)
                                                                                     .Average(p => p.DisgustShare))
                                                    - Convert.ToDouble(dialogueFrames.Where(p => p.SadnessShare != null)
                                                                                     .Average(p => p.SadnessShare)))
                                             + Convert.ToDouble(
                                                 dialogueAudio.PositiveTone * 0.5 - dialogueAudio.NegativeTone * 0.3)
                                             + (dialogueFrames
                                                   .Count(p => p.YawShare >= faceYawMin && p.YawShare <= faceYawMax) *
                                                100 / (dialogueFrames.Count() + 1) / 3 - 27)
                                             + Convert.ToDouble(PositiveTextTone / 4 - 18));

                if (totalScore > 99) totalScore = 99;

                if (totalScore < 10) totalScore = 10;
            }
            catch
            {
                totalScore = 0;
            }

            return totalScore;
        }

        public static Int32 BorderMoodCalculate(DialogueFrame dialogueFrame, DialogueInterval dialogueInterval)
        {
            var faceYawMax = Convert.ToDouble(EnvVar.Get("FaceYawMax"));
            var faceYawMin = Convert.ToDouble(EnvVar.Get("FaceYawMin"));
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

        public static Int32 BorderMoodCalculateList(List<DialogueFrame> dialogueFrame,
            List<DialogueInterval> dialogueInterval, Int32 averageScore)
        {
            var score = 0;
            var length = 0;
            for (var i = 0; i < dialogueFrame.Count(); i++)
                try
                {
                    var result = BorderMoodCalculate(dialogueFrame[i], dialogueInterval[i]);
                    score += result != 0 ? result : 0;
                    length += result != 0 ? 1 : 0;
                }
                catch
                {
                }

            score = score != 0 ? score / length : averageScore;
            return score;
        }

        public async void RewriteSatisfactionScore(String dialogueId)
        {
            var satisfactionScore = new DialogueClientSatisfaction();
            try
            {
                satisfactionScore =
                    await _repository.FindOneByConditionAsync<DialogueClientSatisfaction>(p =>
                        p.DialogueId.ToString() == dialogueId);
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
                    clientWeight = Convert.ToDouble(EnvVar.Get("ClientWeight"));
                }

                if (satisfactionScore.MeetingExpectationsByEmpoyee != null)
                {
                    employeeWeight = Convert.ToDouble(EnvVar.Get("EmployeeWeight"));
                    employeeScore = satisfactionScore.MeetingExpectationsByEmpoyee;
                    if (satisfactionScore.BegMoodByEmpoyee != null)
                        employeeBegScore = satisfactionScore.BegMoodByEmpoyee;

                    if (satisfactionScore.EndMoodByEmpoyee != null)
                        employeeEndScore = satisfactionScore.EndMoodByEmpoyee;
                }

                if (satisfactionScore.MeetingExpectationsByTeacher != null ||
                    satisfactionScore.MeetingExpectationsByTeacher != 0)
                {
                    teacherWeight = Convert.ToDouble(EnvVar.Get("TeacherWeight"));
                    teacherScore = satisfactionScore.MeetingExpectationsByTeacher;
                    if (satisfactionScore.BegMoodByTeacher != null)
                        teacherBegScore = satisfactionScore.BegMoodByTeacher;

                    if (satisfactionScore.EndMoodByTeacher != null)
                        teacherEndScore = satisfactionScore.EndMoodByTeacher;
                }

                if (satisfactionScore.MeetingExpectationsByNN != null)
                {
                    nNWeight = Convert.ToDouble(EnvVar.Get("NNWeight"));
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
                await _repository.SaveAsync();
            }
        }
    }
}