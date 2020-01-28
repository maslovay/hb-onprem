using System;
using System.Linq;
using HBData.Models;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using Microsoft.EntityFrameworkCore;
using FillingSatisfactionService.Utils;

namespace FillingSatisfactionService.Services
{
    public class FillingSatisfactionServiceCalculation
    {

        private readonly TotalScoreCalculations _calc;
        private readonly TotalScoreRecalculations _recalc;
        private readonly RecordsContext _context;

        public FillingSatisfactionServiceCalculation(IServiceScopeFactory factory,
            TotalScoreCalculations calc,
            TotalScoreRecalculations recalc
            )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _calc = calc;
            _recalc = recalc;
        }

        public void DialogueSatisfactionScoreCalculate(Guid dialogueId)
        {
            try
            {
                var dialogue = _context.Dialogues
                    .Include(p => p.DialogueFrame)
                    .Include(p => p.DialogueAudio)
                    .Include(p => p.DialogueSpeech)
                    .Include(p => p.DialogueInterval)
                    .Include(p => p.DialogueVisual)
                    .Include(p => p.DialogueClientSatisfaction)
                    .FirstOrDefault(p => p.DialogueId == dialogueId);

                var pollAnswer = _context.CampaignContentAnswers.Where(x => x.Time >= dialogue.BegTime
                        && x.Time <= dialogue.EndTime
                        && x.ApplicationUserId == dialogue.ApplicationUserId).ToList();

                var framesCountPeriod = Math.Min(10, dialogue.DialogueFrame.Count() / 3);
                var intervalCountPeriod = Math.Min(10, dialogue.DialogueInterval.Count() / 3);

                var totalsScoreNN = _calc.TotalDialogueScoreCalculate(dialogue);
                var begScoreNN = _calc.BorderDialogueScoreCalculate(
                    dialogue.DialogueFrame.OrderBy(p => p.Time).Take(framesCountPeriod).ToList(),
                    dialogue.DialogueInterval.OrderBy(p => p.BegTime).Take(intervalCountPeriod).ToList(),
                    dialogue.DialogueSpeech.FirstOrDefault()    
                );
                var endScoreNN = _calc.BorderDialogueScoreCalculate(
                    dialogue.DialogueFrame.OrderByDescending(p => p.Time).Take(framesCountPeriod).ToList(),
                    dialogue.DialogueInterval.OrderByDescending(p => p.BegTime).Take(intervalCountPeriod).ToList(),
                    dialogue.DialogueSpeech.FirstOrDefault()    
                );
                var clientScore = _calc.ClientDialogueScoreCalculate(dialogue, pollAnswer);
                System.Console.WriteLine($"{totalsScoreNN}, {begScoreNN}, {endScoreNN}, {clientScore}");
                var dialogueSatisfaction = dialogue.DialogueClientSatisfaction.FirstOrDefault();

                if (dialogue.DialogueClientSatisfaction.Any())
                {
                    dialogueSatisfaction.MeetingExpectationsByClient = clientScore;
                    dialogueSatisfaction.MeetingExpectationsByNN = totalsScoreNN;
                    dialogueSatisfaction.BegMoodByNN = begScoreNN;
                    dialogueSatisfaction.EndMoodByNN = endScoreNN;
                    dialogueSatisfaction.MeetingExpectationsTotal = _recalc.RecalculateTotalScore(dialogueSatisfaction);
                    dialogueSatisfaction.BegMoodTotal = _recalc.RecalculateBegTotalScore(dialogueSatisfaction);
                    dialogueSatisfaction.EndMoodTotal = _recalc.RecalculateEndTotalScore(dialogueSatisfaction);
                }
                else
                {
                    var clientSatisfaction = new DialogueClientSatisfaction
                    {
                        DialogueClientSatisfactionId = Guid.NewGuid(),
                        DialogueId = dialogue.DialogueId,
                        MeetingExpectationsByNN = Math.Max((double) totalsScoreNN, 35),
                        BegMoodByNN = Math.Max((double) begScoreNN, 35 ),
                        EndMoodByNN = Math.Max((double) endScoreNN, 35 ),
                        MeetingExpectationsByClient = clientScore
                    };
                    clientSatisfaction.MeetingExpectationsTotal = _recalc.RecalculateTotalScore(clientSatisfaction);
                    clientSatisfaction.MeetingExpectationsTotal = _recalc.RecalculateTotalScore(clientSatisfaction);
                    clientSatisfaction.MeetingExpectationsTotal = _recalc.RecalculateTotalScore(clientSatisfaction);
                    _context.DialogueClientSatisfactions.Add(clientSatisfaction);
                }
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}