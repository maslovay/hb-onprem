using System;
using System.Linq;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace FillingSatisfactionService
{
    public class FillingSatisfaction
    {
        private readonly IGenericRepository _repository;

        public FillingSatisfaction(IServiceScopeFactory factory)
        {
            _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
        }

        public async void Run()
        {
            var dialogueId = Guid.NewGuid();
            try
            {
                void RecordDialogueClientSatisfaction(Guid DialogueClientSatisfactionId, Guid? DialogueId,
                    double? MeetingExpectationsTotal, double? BegMoodTotal, double? EndMoodTotal,
                    double? MeetingExpectationsByNN, double? BegMoodByNN, double? EndMoodByNN)
                {
                    var emp = new DialogueClientSatisfaction
                    {
                        DialogueClientSatisfactionId = DialogueClientSatisfactionId,
                        DialogueId = DialogueId,
                        MeetingExpectationsTotal = MeetingExpectationsTotal,
                        BegMoodTotal = BegMoodTotal,
                        EndMoodTotal = EndMoodTotal,
                        MeetingExpectationsByNN = MeetingExpectationsByNN,
                        BegMoodByNN = BegMoodByNN,
                        EndMoodByNN = EndMoodByNN
                    };
                    _repository.Create(emp);
                    _repository.Save();
                }

                var dialogueFrame = await _repository.FindByConditionAsync<DialogueFrame>(p => p.DialogueId == dialogueId);
                var dialogueAudio = new DialogueAudio();

                double? positiveTextTone;
                try
                {
                    dialogueAudio =
                        await _repository.FindOneByConditionAsync<DialogueAudio>(p => p.DialogueId == dialogueId);
                }
                catch
                {
                    dialogueAudio = null;
                }

                try
                {
                    positiveTextTone = _repository
                                      .Get<DialogueSpeech>().First(p => p.DialogueId == dialogueId).PositiveShare;
                }
                catch
                {
                    positiveTextTone = null;
                }

                var dialogueInterval =
                    await _repository.FindByConditionAsync<DialogueInterval>(p => p.DialogueId == dialogueId);
                                      
                var meetingExpectationsByNN =
                    HBLib.Service.Calculations.TotalScoreInsideCalculate(dialogueFrame, dialogueAudio,
                        positiveTextTone);
                double? begMoodByNN = 0;
                double? endMoodByNN = 0;
                double nNWeight = 0;

                if (dialogueFrame.Any())
                {
                    var framesCountPeriod = Math.Min(10, (dialogueFrame.Count() / 3));
                    var intervalCountPeriod = Math.Min(10, dialogueInterval.Count() / 3);

                    //BorderMoodCalculateList
                    begMoodByNN = HBLib.Service.Calculations
                                       .BorderMoodCalculateList(dialogueFrame.Take(framesCountPeriod).ToList(),
                                            dialogueInterval.Take(intervalCountPeriod).ToList(),
                                            meetingExpectationsByNN);
                    endMoodByNN = HBLib.Service.Calculations
                                       .BorderMoodCalculateList(
                                            dialogueFrame
                                               .Skip(Math.Max(0, dialogueFrame.Count() - framesCountPeriod)).ToList(),
                                            dialogueInterval
                                               .Skip(Math.Max(0, dialogueInterval.Count() - intervalCountPeriod))
                                               .ToList(),
                                            meetingExpectationsByNN);

                    nNWeight = Convert.ToDouble(EnvVar.Get("NNWeight"));
                }
                else
                {
                    begMoodByNN = null;
                    endMoodByNN = null;
                    nNWeight = 0;
                }

                var satisfactionScore = new DialogueClientSatisfaction();
                try
                {
                    satisfactionScore =
                        HeedbookMessengerStatic.context.DialogueClientSatisfactions.First(p =>
                            p.DialogueId.ToString() == dialogueId);
                }
                catch
                {
                    satisfactionScore = null;
                }

                double clientWeight = 0, employeeWeight = 0, teacherWeight = 0;
                double clientTotalScore = 0, employeeTotalScore = 0, teacherTotalScore = 0;
                double employeeBegScore = 0, teacherBegScore = 0;
                double employeeEndScore = 0, teacherEndScore = 0;
                if (satisfactionScore != null)
                {
                    if (satisfactionScore.MeetingExpectationsByClient != null)
                    {
                        clientTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByClient);
                        clientWeight = Convert.ToDouble(EnvVar.Get("ClientWeight"));
                    }
                    else
                    {
                        clientTotalScore = 0;
                        clientWeight = 0;
                    }

                    if (satisfactionScore.MeetingExpectationsByEmpoyee != null)
                    {
                        employeeTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByEmpoyee);
                        employeeBegScore = Convert.ToDouble(satisfactionScore.BegMoodByEmpoyee);
                        employeeEndScore = Convert.ToDouble(satisfactionScore.EndMoodByEmpoyee);
                        employeeWeight = Convert.ToDouble(EnvVar.Get("EmployeeWeight"));
                    }
                    else
                    {
                        employeeTotalScore = 0;
                        employeeBegScore = 0;
                        employeeEndScore = 0;
                        employeeWeight = 0;
                    }

                    if (satisfactionScore.MeetingExpectationsByTeacher != null)
                    {
                        teacherTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByTeacher);
                        teacherBegScore = Convert.ToDouble(satisfactionScore.BegMoodByTeacher);
                        teacherEndScore = Convert.ToDouble(satisfactionScore.EndMoodByTeacher);
                        teacherWeight = Convert.ToDouble(EnvVar.Get("TeacherWeight"));
                    }
                    else
                    {
                        teacherTotalScore = 0;
                        teacherWeight = 0;
                        teacherBegScore = 0;
                        teacherEndScore = 0;
                    }
                }

                var sumWeight = nNWeight + clientWeight + employeeWeight + teacherWeight;
                var sumWeightExceptClient = nNWeight + employeeWeight + teacherWeight;
                double? meetingExpectationsTotal;
                if (sumWeight != 0)
                {
                    meetingExpectationsTotal =
                        (clientWeight * clientTotalScore + nNWeight * meetingExpectationsByNN +
                         employeeWeight * employeeTotalScore + teacherWeight * teacherTotalScore) / sumWeight;
                }
                else
                {
                    meetingExpectationsTotal = null;
                }

                double? begMoodTotal, endMoodTotal;
                if (sumWeightExceptClient != 0)
                {
                    begMoodTotal =
                        (nNWeight * begMoodByNN + employeeBegScore * employeeWeight + teacherBegScore * teacherWeight) /
                        sumWeightExceptClient;
                    endMoodTotal =
                        (nNWeight * endMoodByNN + employeeEndScore * employeeWeight + teacherEndScore * teacherWeight) /
                        sumWeightExceptClient;
                }
                else
                {
                    begMoodTotal = null;
                    endMoodTotal = null;
                }

                if (satisfactionScore == null)
                {
                    RecordDialogueClientSatisfaction(Guid.NewGuid(), new Guid(dialogueId),
                        meetingExpectationsTotal, begMoodTotal, endMoodTotal,
                        meetingExpectationsByNN, begMoodByNN, endMoodByNN);
                }
                else
                {
                    satisfactionScore.MeetingExpectationsTotal = meetingExpectationsTotal;
                    satisfactionScore.MeetingExpectationsByNN = meetingExpectationsByNN;
                    satisfactionScore.BegMoodTotal = begMoodTotal;
                    satisfactionScore.BegMoodByNN = begMoodByNN;
                    satisfactionScore.EndMoodTotal = endMoodTotal;
                    satisfactionScore.EndMoodByNN = endMoodByNN;
                    HeedbookMessengerStatic.context.SaveChanges();
                }

                //send push notification to user for service quality estimation
                var dialogue =
                    HeedbookMessengerStatic.context.Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                HeedbookMessengerStatic.PushNotificationMessenger.SendNotificationToUser(dialogue.ApplicationUserId,
                    "Please rate the dialogue", "Click on this message to rate service quality",
                    $"/user/dialogueestimation?id={dialogue.DialogueId}");

                log.Info($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                try
                {
                    log.Fatal($"Exception occured {e}");
                    throw;
                }
                catch (Exception e2)
                {
                    log2.Critical($"Exception occured {e2}");
                    throw;
                }
            }
        }
    }
}

}