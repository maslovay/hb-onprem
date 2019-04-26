using System;
using System.Linq;
using System.Threading.Tasks;
using FillingSatisfactionService.Exceptions;
using FillingSatisfactionService.Helper;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FillingSatisfactionService
{
    public class FillingSatisfaction
    {
        private readonly Calculations _calculations;
        private readonly CalculationConfig _config;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;

        public FillingSatisfaction(IServiceScopeFactory factory,
            Calculations calculations,
            CalculationConfig config,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _calculations = calculations;
            _config = config;
            _log = log;
        }

        public async Task Run(Guid dialogueId)
        {
            try
            {
                _log.Info("Function filling satisfaction started.");
                var dialogueFrame =
                    await _repository.FindByConditionAsync<DialogueFrame>(p => p.DialogueId == dialogueId);
                var dialogueAudio = new DialogueAudio();

                Double? positiveTextTone;
                try
                {
                    dialogueAudio =
                        await _repository.FindOneByConditionAsync<DialogueAudio>(p => p.DialogueId == dialogueId);
                }
                catch
                {
                    _log.Warning("Couldn't get dialog audio metrics!");   
                    dialogueAudio = null;
                }

                try
                {
                    positiveTextTone = _repository
                                      .Get<DialogueSpeech>().First(p => p.DialogueId == dialogueId).PositiveShare;
                }
                catch
                {
                    _log.Warning("Couldn't get positivity of text tone metrics!");   
                    positiveTextTone = null;
                }

                var dialogueInterval =
                    await _repository.FindByConditionAsync<DialogueInterval>(p => p.DialogueId == dialogueId);

                var meetingExpectationsByNN =
                    _calculations.TotalScoreInsideCalculate(dialogueFrame, dialogueAudio, positiveTextTone);
                Double? begMoodByNN = 0;
                Double? endMoodByNN = 0;
                Double nNWeight = 0;

                if (dialogueFrame.Any())
                {
                    var framesCountPeriod = Math.Min(10, dialogueFrame.Count() / 3);
                    var intervalCountPeriod = Math.Min(10, dialogueInterval.Count() / 3);

                    //BorderMoodCalculateList
                    begMoodByNN = _calculations
                       .BorderMoodCalculateList(dialogueFrame.Take(framesCountPeriod).ToList(),
                            dialogueInterval.Take(intervalCountPeriod).ToList(),
                            meetingExpectationsByNN);
                    endMoodByNN = _calculations
                       .BorderMoodCalculateList(
                            dialogueFrame
                               .Skip(Math.Max(0, dialogueFrame.Count() - framesCountPeriod)).ToList(),
                            dialogueInterval
                               .Skip(Math.Max(0, dialogueInterval.Count() - intervalCountPeriod))
                               .ToList(),
                            meetingExpectationsByNN);

                    nNWeight = Convert.ToDouble(_config.NNWeightD);
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
                        await _repository.FindOneByConditionAsync<DialogueClientSatisfaction>(p =>
                            p.DialogueId == dialogueId);
                }
                catch
                {
                    _log.Warning("Couldn't get satisfaction score!");   
                    satisfactionScore = null;
                }

                Double clientWeight = 0, employeeWeight = 0, teacherWeight = 0;
                Double clientTotalScore = 0, employeeTotalScore = 0, teacherTotalScore = 0;
                Double employeeBegScore = 0, teacherBegScore = 0;
                Double employeeEndScore = 0, teacherEndScore = 0;
                if (satisfactionScore != null)
                {
                    if (satisfactionScore.MeetingExpectationsByClient != null)
                    {
                        clientTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByClient);
                        clientWeight = Convert.ToDouble(_config.ClientWeight);
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
                        employeeWeight = Convert.ToDouble(_config.EmployeeWeight);
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
                        teacherWeight = Convert.ToDouble(_config.TeacherWeight);
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
                Double? meetingExpectationsTotal;
                if (sumWeight != 0)
                    meetingExpectationsTotal =
                        (clientWeight * clientTotalScore + nNWeight * meetingExpectationsByNN +
                         employeeWeight * employeeTotalScore + teacherWeight * teacherTotalScore) / sumWeight;
                else
                    meetingExpectationsTotal = null;

                Double? begMoodTotal, endMoodTotal;
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
                    var emp = new DialogueClientSatisfaction
                    {
                        DialogueClientSatisfactionId = Guid.NewGuid(),
                        DialogueId = dialogueId,
                        MeetingExpectationsTotal = meetingExpectationsTotal,
                        BegMoodTotal = begMoodTotal,
                        EndMoodTotal = endMoodTotal,
                        MeetingExpectationsByNN = meetingExpectationsByNN,
                        BegMoodByNN = begMoodByNN,
                        EndMoodByNN = endMoodByNN
                    };
                    _repository.Create(emp);
                }
                else
                {
                    satisfactionScore.MeetingExpectationsTotal = meetingExpectationsTotal;
                    satisfactionScore.MeetingExpectationsByNN = meetingExpectationsByNN;
                    satisfactionScore.BegMoodTotal = begMoodTotal;
                    satisfactionScore.BegMoodByNN = begMoodByNN;
                    satisfactionScore.EndMoodTotal = endMoodTotal;
                    satisfactionScore.EndMoodByNN = endMoodByNN;
                }

                _repository.Save();
                _log.Info("Function filling satisfaction finished.");
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                throw new FillingSatisfactionException( e.Message, e );
            }
        }
    }
}