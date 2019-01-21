using System;
using System.Linq;
using HBData.Models;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class FillingSubSatisfaction
    {
        [FunctionName("Filling_Sub_Satisfaction")]
        public static async System.Threading.Tasks.Task RunAsync(
            string mySbMsg,
            ExecutionContext dir,
            ILogger log)
        {
            dynamic msgJs = JsonConvert.DeserializeObject(mySbMsg);
            string dialogueId;
            try
            {
                dialogueId = msgJs["DialogueId"];
            }
            catch (Exception e)
            {
                log.LogError("Failed to read message");
                throw;
            }

            try
            {
                void RecordDialogueClientSatisfaction(Guid DialogueClientSatisfactionId, Guid? DialogueId,
                    double? MeetingExpectationsTotal, double? BegMoodTotal, double? EndMoodTotal,
                    double? MeetingExpectationsByNN, double? BegMoodByNN, double? EndMoodByNN)
                {
                    var emp = new DialogueClientSatisfaction();
                    emp.DialogueClientSatisfactionId = DialogueClientSatisfactionId;
                    emp.DialogueId = DialogueId;
                    emp.MeetingExpectationsTotal = MeetingExpectationsTotal;
                    emp.BegMoodTotal = BegMoodTotal;
                    emp.EndMoodTotal = EndMoodTotal;
                    emp.MeetingExpectationsByNN = MeetingExpectationsByNN;
                    emp.BegMoodByNN = BegMoodByNN;
                    emp.EndMoodByNN = EndMoodByNN;
                    HeedbookMessengerStatic.Context().DialogueClientSatisfactions.Add(emp);
                    HeedbookMessengerStatic.Context().SaveChanges();
                }

                var dialogueFrame = HeedbookMessengerStatic
                                   .Context().DialogueFrames.Where(p => p.DialogueId.ToString() == dialogueId).ToList();
                var dialogueAudio = new DialogueAudio();

                double? positiveTextTone;
                try
                {
                    dialogueAudio = HeedbookMessengerStatic
                                   .Context().DialogueAudios.First(p => p.DialogueId.ToString() == dialogueId);
                }
                catch
                {
                    dialogueAudio = null;
                }

                try
                {
                    positiveTextTone = HeedbookMessengerStatic
                                      .Context().DialogueSpeeches.First(p => p.DialogueId.ToString() == dialogueId)
                                      .PositiveShare;
                }
                catch
                {
                    positiveTextTone = null;
                }

                var dialogueInterval = HeedbookMessengerStatic
                                      .Context().DialogueIntervals.Where(p => p.DialogueId.ToString() == dialogueId)
                                      .ToList();
                var meetingExpectationsByNN =
                    Calculations.TotalScoreInsideCalculate(dialogueFrame, dialogueAudio, positiveTextTone);
                double? begMoodByNN = 0;
                double? endMoodByNN = 0;
                double nNWeight = 0;

                if (dialogueFrame.Count != 0)
                {
                    var framesCountPeriod = Math.Min(10, (dialogueFrame.Count() / 3));
                    var intervalCountPeriod = Math.Min(10, dialogueInterval.Count() / 3);

                    //BorderMoodCalculateList
                    begMoodByNN = Calculations
                       .BorderMoodCalculateList(dialogueFrame.Take(framesCountPeriod).ToList(),
                            dialogueInterval.Take(intervalCountPeriod).ToList(),
                            meetingExpectationsByNN);
                    endMoodByNN = Calculations
                       .BorderMoodCalculateList(
                            dialogueFrame.Skip(Math.Max(0, dialogueFrame.Count() - framesCountPeriod)).ToList(),
                            dialogueInterval.Skip(Math.Max(0, dialogueInterval.Count() - intervalCountPeriod)).ToList(),
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
                    satisfactionScore = HeedbookMessengerStatic
                                       .Context().DialogueClientSatisfactions
                                       .First(p => p.DialogueId.ToString() == dialogueId);
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
                    HeedbookMessengerStatic.Context().SaveChanges();
                }

                //send push notification to user for service quality estimation
                var dialogue = HeedbookMessengerStatic
                              .Context().Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                HeedbookMessengerStatic.PushNotificationMessenger.SendNotificationToUser(dialogue.ApplicationUserId,
                    "Please rate the dialogue", "Click on this message to rate service quality",
                    $"/user/dialogueestimation?id={dialogue.DialogueId}");

                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                try
                {
                    log.LogError($"Exception occured {e}");
                    throw;
                }
                catch (Exception e2)
                {
                    log.LogWarning($"Exception occured {e2}");
                    throw;
                }
            }
        }
    }
}