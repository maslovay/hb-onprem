using System.Runtime.InteropServices;
using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using HBLib.Utils;
using LemmaSharp;
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

                        //  var languageId = _repository
                        //      .Get<Dialogue>()
                        //      .Where(d => d.DialogueId == item.DialogueId)
                        //      .Select(d => d.LanguageId ?? 1)
                        //      .First();
                        var languageId = 2;
                         var speechSpeed = GetSpeechSpeed(recognized, languageId);
                         System.Console.WriteLine(speechSpeed);
                         var dialogueSpeech = new DialogueSpeech
                         {
                             DialogueId = item.DialogueId,
                             IsClient = true,
                             SpeechSpeed = speechSpeed,
                             PositiveShare = default(Double),
                             SilenceShare = GetSilenceShare(recognized, item.BegTime, item.EndTime)
                         };
                         var lemmatizer = LemmatizerFactory.CreateLemmatizer(languageId);
                         
                         foreach(var recWord in recognized)
                         {
                             recWord.Word = lemmatizer.Lemmatize(recWord.Word);
                         }
                         var phrases = await FindPhrases(recognized, item.BegTime, lemmatizer, languageId);
                         System.Console.WriteLine(JsonConvert.SerializeObject(phrases));
                         var phraseCount = new List<DialoguePhraseCount>();

                         foreach (var phraseResult in phrases)
                         {
                             foreach(var phrase in phraseResult)
                             {
                                phraseCount.Add(new DialoguePhraseCount
                                {
                                    DialogueId = item.DialogueId,
                                    IsClient = true,
                                    PhraseCount = phraseResult.Count(p => p.PhraseTypeId == phrase.PhraseTypeId),
                                    PhraseTypeId = phrase.PhraseTypeId
                                });
                             }

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

        private Double GetSilenceShare(List<WordRecognized> words, DateTime begTime, DateTime endTime)
        {
            var wordsDuration = words.Sum(item => Double.Parse(item.EndTime) - Double.Parse(item.StartTime));
            return (endTime.Subtract(begTime).TotalSeconds > 0) ? 100 * Math.Max(endTime.Subtract(begTime).TotalSeconds - wordsDuration, 0.01) / endTime.Subtract(begTime).TotalSeconds : 0;
        }

        private List<PhraseResult> FindWord(List<WordRecognized> text, string word, ILemmatizer lemmatizer, DateTime begTime, Guid phraseId, Guid phraseTypeId)
        {
            var result = new List<PhraseResult>();
            word = lemmatizer.Lemmatize(word.ToLower());
            var index = 0;
            System.Console.WriteLine(JsonConvert.SerializeObject(text));
            System.Console.WriteLine(JsonConvert.SerializeObject(word));

            foreach (var w in text)
            {
                if (lemmatizer.Lemmatize(w.Word) == word)
                {
                    var phraseResult = new PhraseResult{
                        Word = w.Word,
                        BegTime = begTime.AddSeconds(Double.Parse(w.StartTime)),
                        EndTime = begTime.AddSeconds(Double.Parse(w.EndTime)),
                        PhraseId = phraseId,
                        PhraseTypeId = phraseTypeId,
                        Position = index
                    };
                    result.Add(phraseResult);
                }
                index += 1;
            }
            return result;
        }

        private async Task<List<List<PhraseResult>>> FindPhrases(List<WordRecognized> wordRecognized, DateTime begTime, ILemmatizer lemmatizer, Int32 languageId)
        {
            var result = new List<List<PhraseResult>>();
            var wordPos = new List<PhraseResult>();
            var phrases = await _repository.FindByConditionAsync<Phrase>(item => item.LanguageId == languageId);
            foreach(var phrase in phrases){
                var phraseWords = Separator(phrase.PhraseText);
                int minWords = Convert.ToInt32(Math.Round(phrase.Accurancy.Value * phraseWords.Count(), 0));
                if (minWords == 0)
                {
                    minWords = phraseWords.Count();
                }
                var space = phrase.WordsSpace + minWords - 1;
                foreach (var phraseWord in phraseWords)
                {
                    wordPos.AddRange(FindWord(wordRecognized, phraseWord, lemmatizer, begTime, phrase.PhraseId, phrase.PhraseTypeId.Value));
                }
                wordPos = wordPos.OrderBy(p => p.Position).ToList();
                while (wordPos.Count > 0)
                {
                    var el = wordPos[0];
                    var beg = el.Position;
                    var end = el.Position + space;
                    var acceptWords = wordPos.Where(p => p.Position >= beg & p.Position <= end).GroupBy(p => p.Word).Select(x => x.First()).ToList();
                    if (acceptWords.Count() >= minWords)
                    {
                        result.Add(acceptWords);
                        var deleteIndex = wordPos.Where(p => p.Position >= beg & p.Position <= end).ToList().Count();
                        wordPos.RemoveRange(0, deleteIndex);
                    }
                    else
                    {
                        wordPos.RemoveRange(0, 1);
                    }
                }
            }
            return result;
        }

        private static List<string> Separator(string text)
        {
            return text.Split(new char[] { ' ', ',', '.', ')', '(' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}