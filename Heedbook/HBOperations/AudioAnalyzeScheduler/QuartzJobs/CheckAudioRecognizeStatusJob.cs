﻿using AsrHttpClient;
using AudioAnalyzeScheduler.Model;
using HBData;
using HBData.Models;
using HBLib;
using HBLib.Model;
using HBLib.Utils;
using LemmaSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;

namespace AudioAnalyzeScheduler.QuartzJobs
{
    public class CheckAudioRecognizeStatusJob : IJob
    {
        private ElasticClient _log;
        private RecordsContext _context;
        private readonly IServiceScopeFactory _factory;
        private readonly ElasticClientFactory _elasticClientFactory;

        private readonly GoogleConnector _googleConnector;
        // private readonly IGenericRepository _repository;

        public CheckAudioRecognizeStatusJob(IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            GoogleConnector googleConnector)
        {
            // _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _factory = factory;
            _elasticClientFactory = elasticClientFactory;
            _googleConnector = googleConnector;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _factory.CreateScope())
            {
                _log = _elasticClientFactory.GetElasticClient();
                _log.Info("Audio analyze scheduler started.");
                try
                {
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();
                    var audiosReq = _context.FileAudioDialogues
                                         .Include(p => p.Dialogue)
                                         .Include(p => p.Dialogue.ApplicationUser)
                                         .Include(p => p.Dialogue.ApplicationUser.Company)
                                         .Where(p => p.StatusId == 6);
                                        //  .ToList();
                    if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud")
                    {
                        audiosReq = audiosReq.Where(p => !String.IsNullOrEmpty(p.TransactionId));
                    }
                    var audios = audiosReq.ToList();

                    var phrases = _context.Phrases.ToList();

                    var dialogueSpeeches = new List<DialogueSpeech>();
                    var dialogueWords = new List<DialogueWord>();
                    var phraseCounts = new List<DialoguePhraseCount>();
                    var dialoguePhrases = new List<DialoguePhrase>();
                    // System.Console.WriteLine($"Audios count - {audios.Count()}");
                    foreach (var audio in audios)
                    {
                        _log.Info($"Processing {audio.DialogueId}");
                     
                        var recognized = new List<WordRecognized>();

                        _log.Info($"Infrastructure: {Environment.GetEnvironmentVariable("INFRASTRUCTURE")}");
                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud")
                        {
                            try
                            {
                                var sttResults = await _googleConnector.GetGoogleSTTResults(audio.TransactionId);
                                var differenceHour = (DateTime.UtcNow - audio.CreationTime).Hours;

                                if (sttResults.Response == null && differenceHour >= 1)
                                {
                                    audio.StatusId = 8;
                                    _log.Error($"Error with stt results for {audio.DialogueId}");
                                }
                                else
                                {
                                    if (sttResults.Response.Results.Any())
                                    {
                                        _log.Info($"{JsonConvert.SerializeObject(sttResults.Response.Results)}");
                                        sttResults.Response.Results
                                            .ForEach(res => res.Alternatives
                                                                .ForEach(alt => alt.Words
                                                                                    .ForEach(word =>
                                                                                    {
                                                                                        if (word == null)
                                                                                        {
                                                                                            _log.Error("word = NULL!");
                                                                                            return;
                                                                                        }

                                                                                        if (word.EndTime == null)
                                                                                        {
                                                                                            _log.Error("No word.EndTime!");
                                                                                            return;
                                                                                        }

                                                                                        if (word.StartTime == null)
                                                                                        {
                                                                                            _log.Error("No word.StartTime!");
                                                                                            return;
                                                                                        }

                                                                                        word.EndTime =
                                                                                            word.EndTime.Replace('s', ' ')
                                                                                                .Replace('.', ',');
                                                                                        word.StartTime =
                                                                                            word.StartTime.Replace('s', ' ')
                                                                                                .Replace('.', ',');
                                                                                        recognized.Add(word);
                                                                                    })));
                                        audio.STTResult = JsonConvert.SerializeObject(recognized);
                                    }
                                    else
                                    {
                                        if (sttResults.Response.Results != null)
                                        {
                                            audio.StatusId = 7;
                                            audio.STTResult = "[]";
                                        }
                                        else
                                        {
                                            _log.Info($"Stt result is null {sttResults.Response.Results == null}");
                                        }
                                    }
                                    _log.Info($"Has items: {sttResults.Response.Results.Any()}");
                                }
                            }
                            catch (Exception e)
                            {
                                _log.Error($"Error parsing result {e}");
                                audio.StatusId = 8;
                            }
                        }

                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "OnPrem")
                        {
                            var asrResults = JsonConvert.DeserializeObject<List<AsrResult>>(audio.STTResult);
                            asrResults.ForEach(word =>
                            {
                                recognized.Add(new WordRecognized
                                {
                                    Word = word.Word,
                                    StartTime = word.Time.ToString(CultureInfo.InvariantCulture),
                                    EndTime = (word.Time + word.Duration).ToString(CultureInfo.InvariantCulture)
                                });
                            });
                            _log.Info($"Has items: {asrResults.Any()}");
                        }

                        if (recognized.Any())
                        {
                            var languageId = (int) audio.Dialogue.ApplicationUser.Company.LanguageId;
                            var speechSpeed = GetSpeechSpeed(recognized, languageId);
                            _log.Info($"Speech speed: {speechSpeed}");

                            var newSpeech = new DialogueSpeech
                            {
                                DialogueId = audio.DialogueId,
                                IsClient = true,
                                SpeechSpeed = speechSpeed,
                                PositiveShare = default(Double),
                                SilenceShare = GetSilenceShare(recognized, audio.BegTime, audio.EndTime)
                            };

                            dialogueSpeeches.Add(newSpeech);

                            var lemmatizer = LemmatizerFactory.CreateLemmatizer(languageId);
                            var phraseCount = new List<DialoguePhraseCount>();
                            var phraseCounter = new Dictionary<Guid, Int32>();
                            var words = new List<PhraseResult>();

                            foreach (var phrase in phrases)
                            {
                                var foundPhrases =
                                    await FindPhrases(recognized, phrase, audio.BegTime, lemmatizer, languageId);
                                foundPhrases.ForEach(f => words.AddRange(f));
                                if (phraseCounter.Keys.Contains(phrase.PhraseTypeId.Value))
                                    phraseCounter[phrase.PhraseTypeId.Value] += foundPhrases.Count();
                                else
                                    phraseCounter[phrase.PhraseTypeId.Value] = foundPhrases.Count();

                                if (foundPhrases.Any())
                                {
                                    dialoguePhrases.Add(new DialoguePhrase
                                    {
                                        DialoguePhraseId = Guid.NewGuid(),
                                        DialogueId = audio.DialogueId,
                                        PhraseTypeId = phrase.PhraseTypeId,
                                        PhraseId = phrase.PhraseId
                                    });
                                }
                            }

                            foreach (var key in phraseCounter.Keys)
                                phraseCount.Add(new DialoguePhraseCount
                                {
                                    DialogueId = audio.DialogueId,
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
                                        BegTime = audio.BegTime.AddSeconds(Double.Parse(r.StartTime,
                                            CultureInfo.InvariantCulture)),
                                        EndTime = audio.BegTime.AddSeconds(Double.Parse(r.EndTime,
                                            CultureInfo.InvariantCulture))
                                    });
                            });

                            newSpeech.PositiveShare = GetPositiveShareInText(recognized.Select(r => r.Word).ToList());

                            words = words.GroupBy(item => new
                            {
                                item.BegTime,
                                item.Word
                            })
                            .Select(item => item.FirstOrDefault())
                            .ToList();
                            
                            words.Sort( (w0, w1) => (int)((w0.BegTime - w1.BegTime).TotalMilliseconds));
                            
                            dialogueWords.Add(new DialogueWord
                            {
                                DialogueId = audio.DialogueId,
                                IsClient = true,
                                Words = JsonConvert.SerializeObject(words)
                            });
                            phraseCounts.AddRange(phraseCount);
                            _log.Info("Asr stt results is not empty. Everything is ok!");
                            
                            
                            if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud") audio.StatusId = 7;
                        }
                        else
                        {
                            _log.Info("Asr stt results is empty");
                        }

                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "OnPrem") audio.StatusId = 7;
                    }

                    _context.DialoguePhrases.AddRange(dialoguePhrases);
                    _context.DialogueSpeechs.AddRange(dialogueSpeeches);
                    _context.DialogueWords.AddRange(dialogueWords);
                    _context.DialoguePhraseCounts.AddRange(phraseCounts);
                    _context.SaveChanges();
                    _log.Info("Scheduler ended.");
                }
                catch (Exception e)
                {
                    _log.Fatal($"Exception occured {e}");
                }
            }
        }

        private double GetPositiveShareInText(List<string> recognizedWords)
        {
            var result = 0.0;
            try
            {
                var sentence = string.Join(" ", recognizedWords);
                var posShareStrg = RunPython.Run("GetPositiveShare.py", "sentimental", "3", sentence);

                if (!posShareStrg.Item2.Trim().IsNullOrEmpty())
                    _log.Error("RunPython err string: " + posShareStrg.Item2);
                else
                    _log.Info("RunPython result string: " + posShareStrg.Item1);

                result = double.Parse(posShareStrg.Item1.Trim());
            }
            catch (Exception ex)
            {
                _log.Error("GetPositiveShareInText exception: " + ex.Message);
                result = 0.0;
            }

            return result;
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
            var wordsDuration = words.Sum(item =>
                Double.Parse(item.EndTime, CultureInfo.InvariantCulture) -
                Double.Parse(item.StartTime, CultureInfo.InvariantCulture));
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
            // Console.WriteLine(JsonConvert.SerializeObject(text));
            // Console.WriteLine(JsonConvert.SerializeObject(word));

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
            return text.Split(new[] {' ', ',', '.', ')', '('}, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}