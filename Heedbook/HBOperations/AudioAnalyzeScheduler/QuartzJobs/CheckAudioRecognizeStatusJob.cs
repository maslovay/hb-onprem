using AsrHttpClient;
using AudioAnalyzeScheduler.Model;
using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using HBLib.Utils;
using LemmaSharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AudioAnalyzeScheduler.QuartzJobs
{
    public class CheckAudioRecognizeStatusJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;

        public CheckAudioRecognizeStatusJob(IServiceScopeFactory factory,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _log = log;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _log.Info("Audion analyze scheduler started.");
            var audios = await _repository.FindByConditionAsync<FileAudioDialogue>(item => item.StatusId == 6);
            var audiosArray = audios.ToArray();
            if (!audiosArray.Any()) _log.Info("No audios found");
            var tasks = audiosArray.Select(item =>
            {
                return Task.Run(async () =>
                {
                    var asrResults = JsonConvert.DeserializeObject<List<AsrResult>>(item.STTResult);
                    Console.WriteLine($"Has items: {asrResults.Any()}");
                    var recognized = new List<WordRecognized>();
                    if (asrResults.Any())
                    {
                        asrResults
                            .ForEach(word =>
                            {
                                recognized.Add(new WordRecognized
                                {
                                    Word = word.Word,
                                    StartTime = word.Time.ToString(CultureInfo.InvariantCulture),
                                    EndTime = (word.Time + word.Duration).ToString(CultureInfo.InvariantCulture)
                                });
                            });
                        var languageId = _repository.GetWithInclude<Dialogue>(i => i.DialogueId == item.DialogueId,
                                i => i.Language)
                            .Select(i => i.Language.LanguageId)
                            .First();
                        var speechSpeed = GetSpeechSpeed(recognized, languageId);
                        _log.Info($"Speech speed: {speechSpeed}");
                        var dialogueSpeech = new DialogueSpeech
                        {
                            DialogueId = item.DialogueId,
                            IsClient = true,
                            SpeechSpeed = speechSpeed,
                            PositiveShare = default(Double),
                            SilenceShare = GetSilenceShare(recognized, item.BegTime, item.EndTime)
                        };
                        
                        var lemmatizer = LemmatizerFactory.CreateLemmatizer(languageId);
                        var phrases = await _repository.FindAllAsync<Phrase>();
                        var phraseCount = new List<DialoguePhraseCount>();
                        var phraseCounter = new Dictionary<Guid, Int32>();
                        var words = new List<PhraseResult>();
                        
                        foreach (var phrase in phrases)
                        {
                            var foundPhrases =
                                await FindPhrases(recognized, phrase, item.BegTime, lemmatizer, languageId);
                            Console.WriteLine(JsonConvert.SerializeObject(phrases));
                            foundPhrases.ForEach(f => words.AddRange(f));
                            
                            if (phraseCounter.Keys.Contains(phrase.PhraseTypeId.Value))
                                phraseCounter[phrase.PhraseTypeId.Value] += foundPhrases.Count();
                            else
                                phraseCounter[phrase.PhraseTypeId.Value] = foundPhrases.Count();
                        }

                        foreach (var key in phraseCounter.Keys)
                            phraseCount.Add(new DialoguePhraseCount
                            {
                                DialogueId = item.DialogueId,
                                PhraseTypeId = key,
                                PhraseCount = phraseCounter[key],
                                IsClient = true
                            });
                        
                        recognized.ForEach(r =>
                        {
                            if (words.All(w => w.Word != r.Word))
                                words.Add(new PhraseResult
                                {
                                    Word = r.Word,
                                    BegTime = item.BegTime.AddSeconds(Double.Parse(r.StartTime, CultureInfo.InvariantCulture)),
                                    EndTime = item.BegTime.AddSeconds(Double.Parse(r.EndTime, CultureInfo.InvariantCulture))
                                });
                        });

                        await _repository.CreateAsync(new DialogueWord
                        {
                            DialogueId = item.DialogueId,
                            IsClient = true,
                            Words = JsonConvert.SerializeObject(words)
                        });
                        await _repository.CreateAsync(dialogueSpeech);
                        await _repository.BulkInsertAsync(phraseCount);

                        _log.Info("Asr stt results is not empty. Everything is ok!");
                    }
                    else
                    {
                        _log.Info("Asr stt results is empty");
                    }
                    Console.WriteLine("status id 7 ");
                    item.StatusId = 7;
                });
            }).ToList();

            await Task.WhenAll(tasks);
            _repository.Save();
            Console.WriteLine("Data saved.");
            _log.Info("Scheduler ended.");
        }

        private Double GetSpeechSpeed(IReadOnlyCollection<WordRecognized> words, Int32 languageId)
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

        private Double GetSilenceShare(IEnumerable<WordRecognized> words, DateTime begTime, DateTime endTime)
        {
            var wordsDuration = words.Sum(item => Double.Parse(item.EndTime, CultureInfo.InvariantCulture) - Double.Parse(item.StartTime, CultureInfo.InvariantCulture));
            return endTime.Subtract(begTime).TotalSeconds > 0
                ? 100 * Math.Max(endTime.Subtract(begTime).TotalSeconds - wordsDuration, 0.01) /
                  endTime.Subtract(begTime).TotalSeconds
                : 0;
        }

        private List<PhraseResult> FindWord(IReadOnlyCollection<WordRecognized> text, String word, ILemmatizer lemmatizer,
            DateTime begTime, Guid phraseId, Guid phraseTypeId)
        {
            var result = new List<PhraseResult>();
            word = lemmatizer.Lemmatize(word.ToLower());
            var index = 0;
            Console.WriteLine(JsonConvert.SerializeObject(text));
            Console.WriteLine(JsonConvert.SerializeObject(word));

            foreach (var w in text)
            {
                if (lemmatizer.Lemmatize(w.Word) == word)
                {
                    var phraseResult = new PhraseResult
                    {
                        Word = w.Word,
                        BegTime = begTime.AddSeconds(Double.Parse(w.StartTime, CultureInfo.InvariantCulture)),
                        EndTime = begTime.AddSeconds(Double.Parse(w.EndTime, CultureInfo.InvariantCulture)),
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

        private async Task<List<List<PhraseResult>>> FindPhrases(List<WordRecognized> wordRecognized, Phrase phrase,
            DateTime begTime, ILemmatizer lemmatizer, Int32 languageId)
        {
            var result = new List<List<PhraseResult>>();
            var wordPos = new List<PhraseResult>();
            var phraseWords = Separator(phrase.PhraseText);
            var accuracy = phrase.Accurancy ?? 0;
            var minWords = Convert.ToInt32(Math.Round(accuracy * phraseWords.Count(), 0));
            if (minWords == 0) minWords = phraseWords.Count();
            var space = phrase.WordsSpace + minWords - 1;
            foreach (var phraseWord in phraseWords)
                wordPos.AddRange(FindWord(wordRecognized, phraseWord, lemmatizer, begTime, phrase.PhraseId,
                    phrase.PhraseTypeId.Value));
            wordPos = wordPos.OrderBy(p => p.Position).ToList();
            while (wordPos.Count > 0)
            {
                var el = wordPos[0];
                var beg = el.Position;
                var end = el.Position + space;
                var acceptWords = wordPos.Where(p => (p.Position >= beg) & (p.Position <= end)).GroupBy(p => p.Word)
                                         .Select(x => x.First()).ToList();
                
                if (acceptWords.Count() >= minWords)
                {
                    result.Add(acceptWords);
                    var deleteIndex = wordPos.Where(p => (p.Position >= beg) & (p.Position <= end)).ToList().Count();
                    wordPos.RemoveRange(0, deleteIndex);
                }
                else
                {
                    wordPos.RemoveRange(0, 1);
                }
            }

            return result;
        }

        private static List<String> Separator(String text)
        {
            return text.Split(new[] { ' ', ',', '.', ')', '(' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}