using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using LemmaSharp;
using LemmaSharp.Classes;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
namespace OperationService.Legacy
{
    public static class FillingSubPhrases
    {
        [FunctionName("Filling_Sub_Phrases")]
        public static void Run(
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
            catch
            {
                log.LogError($"Failed to read message {mySbMsg}");
                throw;
            }

            try
            {
                // GET WORDS, PHRASES AND LANGUAGE FROM MSSQL
                var words = HeedbookMessengerStatic
                           .Context().DialogueWords.Where(p => p.DialogueId.ToString() == dialogueId)
                           .OrderBy(p => p.BegTime).Select(p => new {Word = p.Word}).ToList();
                var languageId = HeedbookMessengerStatic
                                .Context().Dialogues.Include(p => p.Language)
                                .First(p => p.DialogueId.ToString() == dialogueId).LanguageId;
                string companyId = HeedbookMessengerStatic.Context().Dialogues
                                                          .Include(p => p.ApplicationUser)
                                                          .First(p => p.DialogueId.ToString() == dialogueId)
                                                          .ApplicationUser.CompanyId.ToString();

                var phrases = HeedbookMessengerStatic.Context().PhraseCompanies
                                                     .Include(p => p.Phrase)
                                                     .Where(p => p.CompanyId.ToString() == companyId &
                                                                 p.Phrase.LanguageId == languageId)
                                                     .Select(p => new
                                                      {
                                                          PhraseText = p.Phrase.PhraseText,
                                                          Accurancy = p.Phrase.Accurancy, PhraseId = p.Phrase.PhraseId,
                                                          WordsSpace = p.Phrase.WordsSpace,
                                                          PhraseTypeId = p.Phrase.PhraseTypeId
                                                      })
                                                     .ToList().Distinct().ToList();

                if (words.Count == 0)
                {
                    log.LogInformation($"Function finished: {dir.FunctionName}");
                }
                else
                {
                    log.LogInformation("Creating lemmatizer");
                    // CREATE LEMMATIZER CORRESPONDING LANGUAGE ID

                    log.LogInformation("4");
                    ILemmatizer lmtz;
                    switch (languageId)
                    {
                        case 1:
                            lmtz = new Lemmatizer();
                            break;
                        case 2:
                            lmtz = new LemmatizerPrebuiltCompact(LanguagePrebuilt.Russian);
                            break;
                        case 3:
                            lmtz = new LemmatizerPrebuiltCompact(LanguagePrebuilt.Spanish);
                            break;
                        case 4:
                            lmtz = new LemmatizerPrebuiltCompact(LanguagePrebuilt.French);
                            break;
                        case 5:
                            lmtz = new LemmatizerPrebuiltCompact(LanguagePrebuilt.Italian);
                            break;
                        case 8:
                            lmtz = new LemmatizerPrebuiltCompact(LanguagePrebuilt.German);
                            break;
                        case 10:
                            lmtz = new LemmatizerPrebuiltCompact(LanguagePrebuilt.Hungarian);
                            break;
                        default:
                            return;
                    }

                    // FROM WORDS CREATE ORIGINAL TEXT AND NORMAL TEXT
                    var originalText = "";
                    var text = "";
                    foreach (var word in words)
                    {
                        if (word.Word != "" && word.Word != null)
                        {
                            text += lmtz.Lemmatize(word.Word.ToLower()) + " ";
                            originalText += word.Word.ToLower() + " ";
                        }
                    }

                    log2.Info($"{text}");

                    var originalWords = Separator(originalText);
                    log.Info($"Dialogue text - {text}");
                    var phraseCount = new Dictionary<int, int>();
                    var phraseTypeId =
                        HeedbookMessengerStatic.context.PhraseTypes.Select(p => new {typeId = p.PhraseTypeId});

                    foreach (var phrase in phrases)
                    {
                        // FIND ALL PHRASES IN TEXT
                        var phrs = FindPhrase(text, phrase.PhraseText, lmtz, Convert.ToDouble(phrase.Accurancy),
                            Convert.ToInt32(phrase.WordsSpace), log2);
                        if (phrs.Count() != 0)
                        {
                            foreach (var phr in phrs)
                            {
                                // ADD DATA TO DIALOGUE PHRASE
                                var dialoguePhrase = new DialoguePhrase();
                                dialoguePhrase.DialogueId = Guid.Parse(dialogueId);
                                dialoguePhrase.PhraseId = phrase.PhraseId;
                                dialoguePhrase.IsClient = true;
                                HeedbookMessengerStatic.context.DialoguePhrases.Add(dialoguePhrase);

                                // ADD DATA TO DIALOGUE PHRASE PLACE
                                foreach (var phrWord in phr)
                                {
                                    var dialoguePhrasePlace = new DialoguePhrasePlace();
                                    dialoguePhrasePlace.DialogueId = Guid.Parse(dialogueId);
                                    dialoguePhrasePlace.PhraseId = phrase.PhraseId;
                                    dialoguePhrasePlace.Synonyn = false;
                                    dialoguePhrasePlace.SynonynText = originalWords[phrWord.position];
                                    dialoguePhrasePlace.WordPosition = phrWord.position;
                                    HeedbookMessengerStatic.context.DialoguePhrasePlaces.Add(dialoguePhrasePlace);
                                }
                            }

                            // COUNT PHRASES 
                            var phrasePosition = new DialoguePhrasePlace();
                            if (phraseCount.Keys.Contains(Convert.ToInt32(phrase.PhraseTypeId)))
                            {
                                phraseCount[Convert.ToInt32(phrase.PhraseTypeId)] += phrs.Count();
                            }
                            else
                            {
                                phraseCount[Convert.ToInt32(phrase.PhraseTypeId)] = phrs.Count();
                            }
                        }
                    }

                    // ADD DATA TO DIALOGUE PHRASE COUNT
                    foreach (var key in phraseCount.Keys)
                    {
                        var dialoguePhraseCount = new DialoguePhraseCount();
                        dialoguePhraseCount.DialogueId = Guid.Parse(dialogueId);
                        dialoguePhraseCount.IsClient = true;
                        dialoguePhraseCount.PhraseTypeId = key;
                        dialoguePhraseCount.PhraseCount = phraseCount[key];
                        HeedbookMessengerStatic.context.DialoguePhraseCounts.Add(dialoguePhraseCount);
                    }

                    HeedbookMessengerStatic.context.SaveChanges();
                    log.Info($"Function finished: {dir.FunctionName}");
                }
            }
            catch (Exception e)
            {
                log.Fatal($"Exception occured {e}");
                throw e;
            }
        }

        public class WordInfo
        {
            public int position { get; set; }
            public string word { get; set; }
        }

        public static List<string> Separator(string text)
        {
            return text.Split(new char[] {' ', ',', '.', ')', '('}, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static List<WordInfo> FindWord(string text, string word, ILemmatizer lmtz)
        {
            var result = new List<WordInfo>();
            word = lmtz.Lemmatize(word.ToLower());
            var words = Separator(text);
            var index = 0;
            foreach (var w in words)
            {
                if (w == word)
                {
                    var wordsInfo = new WordInfo();
                    wordsInfo.position = index;
                    wordsInfo.word = word;
                    result.Add(wordsInfo);
                }

                index += 1;
            }

            return result;
        }

        public static List<List<WordInfo>> FindPhrase(string text, string phrase, ILemmatizer lmtz, double accurancy,
            int wordSpace, TraceWriter log)
        {
            var result = new List<List<WordInfo>>();
            var wordPos = new List<WordInfo>();
            var phraseWords = Separator(phrase);
            int minWords = Convert.ToInt32(Math.Round(accurancy * phraseWords.Count(), 0));
            if (minWords == 0)
            {
                minWords = phraseWords.Count();
            }

            var space = wordSpace + minWords - 1;
            foreach (var phraseWord in phraseWords)
            {
                wordPos.AddRange(FindWord(text, phraseWord, lmtz));
            }

            wordPos = wordPos.OrderBy(p => p.position).ToList();
            while (wordPos.Count > 0)
            {
                var el = wordPos[0];
                var beg = el.position;
                var end = el.position + space;
                var acceptWords = wordPos.Where(p => p.position >= beg & p.position <= end).GroupBy(p => p.word)
                                         .Select(x => x.First()).ToList();
                if (acceptWords.Count() >= minWords)
                {
                    result.Add(acceptWords);
                    var deleteIndex = wordPos.Where(p => p.position >= beg & p.position <= end).ToList().Count();
                    wordPos.RemoveRange(0, deleteIndex);
                }
                else
                {
                    wordPos.RemoveRange(0, 1);
                }
            }

            return result;
        }
    }
}