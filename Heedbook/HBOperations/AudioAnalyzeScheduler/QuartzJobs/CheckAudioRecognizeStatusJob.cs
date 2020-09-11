using AsrHttpClient;
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
using System.IO;
using System.Linq;
using System.Reflection;
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
            System.Console.WriteLine($"Job started");
            using (var scope = _factory.CreateScope())
            {
                _log = _elasticClientFactory.GetElasticClient();
                System.Console.WriteLine("Audio analyze scheduler started.");
                try
                {
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();
                    var audiosReq = _context.FileAudioDialogues
                                         .Include(p => p.Dialogue)
                                         .Include(p => p.Dialogue.Device)
                                         .Include(p => p.Dialogue.Device.Company)
                                         .Where(p => p.StatusId == 6
                                            && p.DialogueId == Guid.Parse("600cf351-a259-4cba-835d-cd417015ce72"));

                    //  .ToList();
                    await _googleConnector.CheckApiKey();
                    if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud")
                    {
                        audiosReq = audiosReq.Where(p => !String.IsNullOrEmpty(p.TransactionId));
                    }
                    var audios = audiosReq.ToList();
                    System.Console.WriteLine($"number of audios: {audios.Count}");
                    var phrases = _context.Phrases.ToList();

                    
                    
                    // System.Console.WriteLine($"Audios count - {audios.Count()}");
                    foreach (var audio in audios)
                    {
                        var isClient = (audio.FileContainer == "dialogueaudios");
                        var dialoguePhrases = new List<DialoguePhrase>();
                        var dialogueSpeeches = new List<DialogueSpeech>();
                        var dialogueWords = new List<DialogueWord>();
                        var phraseCounts = new List<DialoguePhraseCount>();
                        System.Console.WriteLine($"Processing {audio.DialogueId}");
                     
                        var recognized = new List<WordRecognized>();
                        
                        System.Console.WriteLine($"Infrastructure: {Environment.GetEnvironmentVariable("INFRASTRUCTURE")}");
                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud")
                        {
                            try
                            {
                                var googleAccount = _context.GoogleAccounts.FirstOrDefault(item => item.StatusId == 3);
                                System.Console.WriteLine($"googleAccount is null: {googleAccount is null}");
                                if(googleAccount is null)
                                    break;
                                
                                var sttResults = await _googleConnector.GetGoogleSTTResults(audio.TransactionId);
                                System.Console.WriteLine($"sttResults: {JsonConvert.SerializeObject(sttResults)}");
                                if(sttResults?.Error != null && sttResults?.Error.Status == "NOT_FOUND")
                                {
                                    continue;
                                }
                                    
                                    
                                var differenceHour = (DateTime.UtcNow - audio.CreationTime).Hours;

                                if (((sttResults?.Response == null && differenceHour >= 1)||sttResults?.Response?.Results==null))
                                {
                                    audio.StatusId = 8;
                                    audio.STTResult = "[]";
                                    System.Console.WriteLine($"Error with stt results for {audio.DialogueId}");
                                }
                                else
                                {
                                    if (sttResults.Response.Results.Any())
                                    {
                                        System.Console.WriteLine($"{JsonConvert.SerializeObject(sttResults.Response.Results)}");
                                        sttResults.Response.Results
                                            .ForEach(res => res.Alternatives
                                                                .ForEach(alt => alt.Words
                                                                                    .ForEach(word =>
                                                                                    {
                                                                                        if (word == null)
                                                                                        {
                                                                                            System.Console.WriteLine("word = NULL!");
                                                                                            return;
                                                                                        }

                                                                                        if (word.EndTime == null)
                                                                                        {
                                                                                            System.Console.WriteLine("No word.EndTime!");
                                                                                            return;
                                                                                        }

                                                                                        if (word.StartTime == null)
                                                                                        {
                                                                                            System.Console.WriteLine("No word.StartTime!");
                                                                                            return;
                                                                                        }

                                                                                        word.EndTime =
                                                                                            word.EndTime.Replace('s', ' ');
                                                                                                // .Replace('.', ',');
                                                                                        word.StartTime =
                                                                                            word.StartTime.Replace('s', ' ');
                                                                                                // .Replace('.', ',');
                                                                                        recognized.Add(word);
                                                                                    })));
                                        audio.STTResult = JsonConvert.SerializeObject(recognized);
                                    }
                                    else
                                    {                                        
                                        audio.StatusId = 7;
                                        audio.STTResult = "[]";
                                    }
                                    System.Console.WriteLine($"Has items: {sttResults.Response.Results.Any()}");
                                }
                            }
                            catch (Exception e)
                            {
                                System.Console.WriteLine($"Error parsing result for dialogue {audio.DialogueId}. {e}");
                                audio.StatusId = 8;
                                audio.STTResult = "[]";
                            }
                        }

                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "OnPrem")
                        {
                            var asrResults = string.IsNullOrEmpty(audio.STTResult) ? new List<AsrResult>() : JsonConvert.DeserializeObject<List<AsrResult>>(audio.STTResult);
                            asrResults.ForEach(word =>
                            {
                                recognized.Add(new WordRecognized
                                {
                                    Word = word.Word,
                                    StartTime = word.Time.ToString(CultureInfo.InvariantCulture),
                                    EndTime = (word.Time + word.Duration).ToString(CultureInfo.InvariantCulture)
                                });
                            });
                            System.Console.WriteLine($"Has items: {asrResults.Any()}");
                        }

                        if (recognized.Any())
                        {
                            var languageId = (int) audio.Dialogue.Device.Company.LanguageId;
                            var speechSpeed = GetSpeechSpeed(recognized, languageId, _log);
                            System.Console.WriteLine($"Speech speed: {speechSpeed}");

                            var newSpeech = new DialogueSpeech
                            {
                                DialogueId = audio.DialogueId,
                                IsClient = isClient,
                                SpeechSpeed = speechSpeed,
                                PositiveShare = default(Double),
                                SilenceShare = GetSilenceShare(recognized, audio.BegTime, audio.EndTime, _log)
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
                                    IsClient = isClient
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

                            newSpeech.PositiveShare = GetPositiveShareInText(recognized.Select(r => r.Word).ToList(), audio.DialogueId, isClient);                            
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
                                IsClient = isClient,
                                Words = JsonConvert.SerializeObject(words)
                            });
                            phraseCounts.AddRange(phraseCount);
                            System.Console.WriteLine("Asr stt results is not empty. Everything is ok!");
                            
                            
                            if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud") audio.StatusId = 7;
                        }
                        else
                        {
                            var newSpeech = new DialogueSpeech
                            {
                                DialogueId = audio.DialogueId,
                                IsClient = isClient,
                                SpeechSpeed = 0,
                                PositiveShare = default(Double),
                                SilenceShare = 0
                            };
                            dialogueSpeeches.Add(newSpeech);
                            System.Console.WriteLine("Asr stt results is empty");
                        }

                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "OnPrem") audio.StatusId = 7;
                        
                        _context.DialoguePhrases.AddRange(dialoguePhrases);
                        _context.DialogueSpeechs.AddRange(dialogueSpeeches);
                        _context.DialogueWords.AddRange(dialogueWords);
                        _context.DialoguePhraseCounts.AddRange(phraseCounts);
                        _context.SaveChanges();
                        System.Console.WriteLine($"Finished processing {audio.DialogueId}");
                    }
                    System.Console.WriteLine("Function finished.");
                }
                catch (Exception e)
                {
                    _log.Fatal($"Exception occured {e}");
                }
            }
        }

        private double GetPositiveShareInText(IEnumerable<string> recognizedWords, Guid dialogueId, bool isClient)
        {
            try
            {
                var sentence = string.Join(" ", recognizedWords);
                var posShareStrg = RunPython.Run("GetPositiveShare.py",
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sentimental"), "3",
                    sentence, _log);

                if (!posShareStrg.Item2.Trim().IsNullOrEmpty())
                    System.Console.WriteLine("RunPython err string: " + posShareStrg.Item2 + "  dialogueId: " + dialogueId);
                else
                    System.Console.WriteLine("RunPython result string: " + posShareStrg.Item1 + "  dialogueId: " + dialogueId);

                System.Console.WriteLine("RunPython out string: " + posShareStrg.Item1 + "  dialogueId: " + dialogueId);

                if (!double.TryParse(posShareStrg.Item1.Trim(), out double result))
                {
                    _log.Fatal($"GetPositiveShareInText can't parse string: {posShareStrg.Item1.Trim()} dialogueId: {dialogueId}");
                    // TODO: delete after bug fixing
                    result = 0;
                }

                return result;
            }
            catch (Exception ex)
            {
                _log.Fatal("GetPositiveShareInText exception occurred: " + ex.Message, ex);
                var newSpeech = new DialogueSpeech
                            {
                                DialogueId = dialogueId,
                                IsClient = isClient,
                                SpeechSpeed = 0,
                                PositiveShare = default(Double),
                                SilenceShare = 0
                            };
                _context.DialogueSpeechs.Add(newSpeech);
                _context.SaveChanges();
                throw;
            }
        }

        private Double GetSpeechSpeed(List<WordRecognized> words, Int32 languageId, ElasticClient _log)
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

        private Double GetSilenceShare(List<WordRecognized> words, DateTime begTime, DateTime endTime, ElasticClient _log)
        {
            var wordsDuration = words.Sum(item =>
                Double.Parse(item.EndTime, CultureInfo.InvariantCulture) -
                Double.Parse(item.StartTime, CultureInfo.InvariantCulture));

            return endTime.Subtract(begTime).TotalSeconds > 0
                ? Math.Max(endTime.Subtract(begTime).TotalSeconds - wordsDuration, 0.01) /
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