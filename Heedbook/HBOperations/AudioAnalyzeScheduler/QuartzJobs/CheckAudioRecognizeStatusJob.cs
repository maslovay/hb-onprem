using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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

namespace AudioAnalyzeScheduler.QuartzJobs
{
    public class CheckAudioRecognizeStatusJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IGenericRepository _repository;
        private readonly AsrHttpClient.AsrHttpClient _asrHttpClient;

        public CheckAudioRecognizeStatusJob(IServiceScopeFactory factory,
            AsrHttpClient.AsrHttpClient asrHttpClient,
            INotificationPublisher notificationPublisher,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _notificationPublisher = notificationPublisher;
            _log = log;
            _asrHttpClient = asrHttpClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _log.Info("Audion analyze scheduler started.");
            var audios = await _repository.FindByConditionAsync<FileAudioDialogue>(item => item.StatusId == 6);
            if (!audios.Any()) _log.Info("No audios found");
            var tasks = audios.Select(item =>
            {
                return Task.Run(async () =>
                {
                    var asrResults = await _asrHttpClient.GetAsrResult(item.FileName);
                    Console.WriteLine($"{asrResults}");
                    var differenceHour = (DateTime.Now - item.CreationTime).Hours;
                    if (!asrResults.Any() && differenceHour >= 1)
                    {
                        //8 - error
                        item.StatusId = 8;
                        _repository.Update(item);
                        _log.Info("Error with stt results");
                    }
                    else
                    {
                        var recognized = new List<WordRecognized>();

                        asrResults
                           .ForEach(word =>
                            {
                                recognized.Add(new WordRecognized()
                                {
                                    Word = word.Word,
                                    StartTime = word.Time.ToString(CultureInfo.InvariantCulture),
                                    EndTime = (word.Time + word.Duration).ToString(CultureInfo.InvariantCulture)
                                });
                            });
                        //  var languageId = _repository
                        //      .Get<Dialogue>()
                        //      .Where(d => d.DialogueId == item.DialogueId)
                        //      .Select(d => d.LanguageId ?? 1)
                        //      .First();
                        var languageId = 2;
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
                        item.StatusId = 7;
                        recognized.ForEach(r =>
                        {
                            if (words.All(w => w.Word != r.Word))
                                words.Add(new PhraseResult
                                {
                                    Word = r.Word,
                                    BegTime = item.BegTime.AddSeconds(Double.Parse(r.StartTime)),
                                    EndTime = item.BegTime.AddSeconds(Double.Parse(r.EndTime))
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
                        var @event = new FillingHintsRun
                        {
                            DialogueId = item.DialogueId
                        };
                        _notificationPublisher.Publish(@event);
                        _log.Info("Everything is ok");
                    }
                });
            }).ToList();

            await Task.WhenAll(tasks);
            _repository.Save();
            _log.Info("Scheduler ended.");
        }

        private Double GetSpeechSpeed(List<WordRecognized> words, Int32 languageId)
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
            return endTime.Subtract(begTime).TotalSeconds > 0
                ? 100 * Math.Max(endTime.Subtract(begTime).TotalSeconds - wordsDuration, 0.01) /
                  endTime.Subtract(begTime).TotalSeconds
                : 0;
        }

        private List<PhraseResult> FindWord(List<WordRecognized> text, String word, ILemmatizer lemmatizer,
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

        private async Task<List<List<PhraseResult>>> FindPhrases(List<WordRecognized> wordRecognized, Phrase phrase,
            DateTime begTime, ILemmatizer lemmatizer, Int32 languageId)
        {
            var result = new List<List<PhraseResult>>();
            var wordPos = new List<PhraseResult>();
            var phrases = await _repository.FindByConditionAsync<Phrase>(item => item.LanguageId == languageId);
            var phraseWords = Separator(phrase.PhraseText);
            var minWords = Convert.ToInt32(Math.Round(phrase.Accurancy.Value * phraseWords.Count(), 0));
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
            return text.Split(new[] {' ', ',', '.', ')', '('}, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}