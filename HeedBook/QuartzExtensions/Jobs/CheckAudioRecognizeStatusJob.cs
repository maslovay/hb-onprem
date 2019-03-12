using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phrase = HBData.Models.Phrase;

namespace QuartzExtensions.Jobs
{
    public class CheckAudioRecognizeStatusJob : IJob
    {
        private readonly IGenericRepository _repository;

        private readonly GoogleConnector _googleConnector;

        public CheckAudioRecognizeStatusJob(IServiceScopeFactory scopeFactory,
            GoogleConnector googleConnector)
        {
            var scope = scopeFactory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            _googleConnector = googleConnector;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Scheduler started.");
            var audios = await _repository.FindByConditionAsync<FileAudioDialogue>(item => item.StatusId == 6);
            if(!audios.Any()){
                Console.WriteLine("No audios found");
            }
            var tasks = audios.Select(item =>
            {
                return Task.Run(async () =>
                 {
                     var sttResults = await _googleConnector.GetGoogleSTTResults(item.TransactionId);
                     var differenceHour = (DateTime.Now - item.CreationTime).Hours;
                     if (sttResults.Response == null && differenceHour >= 1)
                     {
                         //8 - error
                         item.StatusId = 8;
                         _repository.Update(item);
                         Console.WriteLine("Error with stt results");
                     }
                     else
                     {
                         var recognized = new List<WordRecognized>();

                         sttResults.Response.Results
                             .ForEach(res => res.Alternatives
                                 .ForEach(alt => alt.Words
                                     .ForEach(word =>
                                     {
                                         word.EndTime = word.EndTime.Replace('s', ' ').Replace('.', ',');
                                         word.StartTime = word.StartTime.Replace('s', ' ').Replace('.', ',');
                                         recognized.Add(word);
                                     })));

                         var languageId = _repository
                             .Get<Dialogue>()
                             .Where(d => d.DialogueId == item.DialogueId)
                             .Select(d => d.LanguageId ?? 1)
                             .First();
                         var speechSpeed = GetSpeechSpeed(recognized, languageId);
                         var phrases = await FindPhrases(recognized, languageId, item.BegTime);
                         var phraseCount = new List<DialoguePhraseCount>();
                         foreach (var phraseResult in phrases)
                         {
                             phraseCount.Add(new DialoguePhraseCount
                             {
                                 DialogueId = item.DialogueId,
                                 IsClient = true,
                                 PhraseCount = phrases.Count(p => p.PhraseTypeId == phraseResult.PhraseTypeId),
                                 PhraseTypeId = phraseResult.PhraseTypeId
                             });
                         }
                         item.StatusId = 7;
                         await _repository.CreateAsync(new DialogueWord
                         {
                             DialogueId = item.DialogueId,
                             IsClient = true,
                             Words = JsonConvert.SerializeObject(phrases)
                         });
                         await _repository.BulkInsertAsync(phraseCount);
                         Console.WriteLine("Everything is ok");
                     }
                 });
            }).ToList();

            await Task.WhenAll(tasks);
            _repository.Save();
            Console.WriteLine("Scheduler ended.");
        }

        private Double GetSpeechSpeed(List<WordRecognized> words, int languageId)
        {
            var vowels = Vowels.VowelsDictionary[languageId];
            var sumTime = words.Sum(item =>
            {
                Double.TryParse(item.EndTime, out var endTime);
                Double.TryParse(item.StartTime, out var startTime);
                return endTime - startTime;
            });
            var vowelsCount = words.Select(item => item.Word.Where(c => vowels.Contains(c))).Count();
            return vowelsCount / sumTime;
        }

        private async Task<List<PhraseResult>> FindPhrases(List<WordRecognized> wordRecognized, int languageId, DateTime begTime)
        {
            var words = wordRecognized.Select(item => new
            {
                Word = LemmatizerFactory.CreateLemmatizer(languageId).Lemmatize(item.Word),
                item.StartTime,
                item.EndTime
            }).ToList();
            var list = new List<PhraseResult>();
            foreach (var word in words)
            {
                var foundedPhrase = await _repository
                    .FindOneByConditionAsync<Phrase>(phrase => phrase.LanguageId == languageId && phrase.PhraseText.Contains(word.Word));
                
                list.Add(new PhraseResult
                {
                    Word = word.Word,
                    BegTime = begTime.AddSeconds(Double.Parse(word.StartTime)),
                    EndTime = begTime.AddSeconds(Double.Parse(word.EndTime)),
                    PhraseId = foundedPhrase?.PhraseId,
                    PhraseTypeId = foundedPhrase?.PhraseTypeId
                });
            }

            return list;
        }
    }
}