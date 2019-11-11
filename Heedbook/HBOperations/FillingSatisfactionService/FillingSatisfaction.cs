using System;
using System.Linq;
using System.Threading.Tasks;
using FillingSatisfactionService.Helper;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using HBLib;
using HBData;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FillingSatisfactionService
{
    public class FillingSatisfaction
    {
        private readonly Calculations _calculations;
        private readonly CalculationConfig _config;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;


        public FillingSatisfaction(IServiceScopeFactory factory,
            Calculations calculations,
            INotificationPublisher notificationPublisher,
            CalculationConfig config,
            ElasticClientFactory elasticClientFactory
            )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _calculations = calculations;
            _notificationPublisher = notificationPublisher;
            _config = config;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Run(Guid dialogueId)
        {
             var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(dialogueId);

            try
            {
                _log.Info("Function started");
                var dialogue = _context.Dialogues
                    .Include(p => p.DialogueFrame)
                    .Include(p => p.DialogueAudio)
                    .Include(p => p.DialogueSpeech)
                    .Include(p => p.DialogueInterval)
                    .Include(p => p.DialogueClientProfile)
                    .FirstOrDefault(p => p.DialogueId == dialogueId);
                var dialogueFrame = dialogue.DialogueFrame;
                var dialogueAudio = dialogue.DialogueAudio.FirstOrDefault();
                var positiveTextTone = dialogue.DialogueSpeech.FirstOrDefault() == null ? null: dialogue.DialogueSpeech.FirstOrDefault().PositiveShare;
                var dialogueInterval = dialogue.DialogueInterval;
                var dialogueClientProfile = dialogue.DialogueClientProfile;

                // var meetingExpectationsByNN =
                // _calculations.TotalScoreInsideCalculate(dialogueFrame, dialogueAudio,
                // positiveTextTone);
                var meetingExpectationsByNN = _calculations.TotalScoreCalculate(dialogue);


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

                    nNWeight = Convert.ToDouble(_config.NnWeight);
                }
                else
                {
                    begMoodByNN = null;
                    endMoodByNN = null;
                    nNWeight = 0;
                }

                DialogueClientSatisfaction satisfactionScore = _context.DialogueClientSatisfactions
                            .FirstOrDefault(p => p.DialogueId == dialogueId);

                double clientWeight = 0, employeeWeight = 0, teacherWeight = 0;
                double clientTotalScore = 0, employeeTotalScore = 0, teacherTotalScore = 0;
                double employeeBegScore = 0, teacherBegScore = 0;
                double employeeEndScore = 0, teacherEndScore = 0;
                if (satisfactionScore != null)
                {
                    satisfactionScore.MeetingExpectationsByClient = _calculations.MeetingExpectationsByClientCalculate(dialogue);
                    clientTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByClient);
                    clientWeight = Convert.ToDouble(_config.ClientWeight);
                    if (satisfactionScore.MeetingExpectationsByEmpoyee != null)
                    {
                        employeeTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByEmpoyee);
                        employeeBegScore = Convert.ToDouble(satisfactionScore.BegMoodByEmpoyee);
                        employeeEndScore = Convert.ToDouble(satisfactionScore.EndMoodByEmpoyee);
                        employeeWeight = Convert.ToDouble(_config.EmployeeWeight);
                    }                  

                    if (satisfactionScore.MeetingExpectationsByTeacher != null)
                    {
                        teacherTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByTeacher);
                        teacherBegScore = Convert.ToDouble(satisfactionScore.BegMoodByTeacher);
                        teacherEndScore = Convert.ToDouble(satisfactionScore.EndMoodByTeacher);
                        teacherWeight = Convert.ToDouble(_config.TeacherWeight);
                    }
                }

                var sumWeight = nNWeight + clientWeight + employeeWeight + teacherWeight;
                var sumWeightExceptClient = nNWeight + employeeWeight + teacherWeight;

                Double? meetingExpectationsTotal = null;
                if (sumWeight != 0)
                {
                    meetingExpectationsTotal =
                        (clientWeight * clientTotalScore + nNWeight * meetingExpectationsByNN +
                         employeeWeight * employeeTotalScore + teacherWeight * teacherTotalScore) / sumWeight;
                }

                Double? begMoodTotal = null, endMoodTotal = null;
                if (sumWeightExceptClient != 0)
                {
                    begMoodTotal =
                        (nNWeight * begMoodByNN + employeeBegScore * employeeWeight + teacherBegScore * teacherWeight) /
                        sumWeightExceptClient;
                    endMoodTotal =
                        (nNWeight * endMoodByNN + employeeEndScore * employeeWeight + teacherEndScore * teacherWeight) /
                        sumWeightExceptClient;
                }

                var random = new Random();
                if (satisfactionScore == null)
                {
                    satisfactionScore = new DialogueClientSatisfaction
                    {
                        DialogueClientSatisfactionId = Guid.NewGuid(),
                        DialogueId = dialogueId                        
                    };
                    _context.DialogueClientSatisfactions.Add(satisfactionScore);
                }
                satisfactionScore.MeetingExpectationsTotal = Math.Max((double) meetingExpectationsTotal, 35);
                satisfactionScore.MeetingExpectationsByNN = Math.Max((double) meetingExpectationsByNN, 35);
                satisfactionScore.BegMoodTotal =  Math.Max((double) begMoodTotal, 35);
                satisfactionScore.BegMoodByNN = Math.Max((double)  begMoodByNN, 35);
                satisfactionScore.EndMoodTotal = Math.Max((double)  endMoodTotal, 35 );
                satisfactionScore.EndMoodByNN =  Math.Max((double)  endMoodByNN, 35);
                satisfactionScore.MeetingExpectationsByClient = _calculations.MeetingExpectationsByClientCalculate(dialogue);
                satisfactionScore.Age = dialogueClientProfile != null? dialogueClientProfile.Average(x => x.Age) : null;
                satisfactionScore.Gender =  dialogueClientProfile != null ?
                    dialogueClientProfile.Count(x => x.Gender == "male") > dialogueClientProfile.Count(x => x.Gender == "female") ? 
                    "male" : "female" : null;
                _log.Info($"Total mood is --- {satisfactionScore.MeetingExpectationsTotal}");


                _context.SaveChanges();
                var @event = new FillingHintsRun
                {
                    DialogueId = dialogueId
                };
                _notificationPublisher.Publish(@event);
                _log.Info("Function filling satisfaction ended.");
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                throw;
            }
        }
    }
}