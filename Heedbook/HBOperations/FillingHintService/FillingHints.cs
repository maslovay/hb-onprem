using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FillingHintService.Model;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using HBLib;
using Microsoft.Azure.Management.Search.Fluent;
using HBData;
using Microsoft.EntityFrameworkCore;

namespace FillingHintService
{
    public class FillingHints
    {
        //private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private readonly ElasticClient _log;
        private RecordsContext _context;

        public FillingHints(IServiceScopeFactory factory,
            ElasticClient log,
            RecordsContext context
            )
        {
            _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _log = log;
            _context = context;
        }

        public async Task Run(Guid dialogueId)
        {
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(dialogueId);
            _log.Info($"Function started: {dialogueId}");
            System.Console.WriteLine($"Function started: {dialogueId}");
            try
            {
                var dialogueHints = _context.DialogueHints.Where(p => p.DialogueId == dialogueId).ToList();
                if(dialogueHints!=null && dialogueHints.Count > 0)
                {
                    _context.DialogueHints.RemoveRange(dialogueHints);
                    _context.SaveChanges();
                    _log.Info($"Old hints have been removed before selecting new hints for dialogue: {dialogueId}, count: {dialogueHints.Count}");
                }                
                           
                var language = _context.Dialogues
                    .Include(p => p.Language)
                    .FirstOrDefault(p => p.DialogueId == dialogueId).Language.LanguageLocalName;
                
                var catalogueHints = _context.CatalogueHints.ToList();
                if (catalogueHints.Any())
                {
                    var hints = catalogueHints.Where(ch => ch.HintCondition != null 
                            && ch.HintText != null)
                        .OrderBy(p => p.CatalogueHintId)
                        .Select(item => new Hint()
                            {
                                HintCondition = JsonConvert.DeserializeObject<List<HintCondition>>(item.HintCondition),
                                HintText = JsonConvert.DeserializeObject<List<HintText>>(item.HintText)
                            })
                        .ToList();
                    
                    foreach (var hintConditions in hints)
                    {
                        foreach (var hintCondition in hintConditions.HintCondition)
                        {
                            Double? resValue = 0;                            
                            if (hintCondition.Table == null || hintCondition.Indexes == null)
                            {
                                continue;
                            }
                            else
                            {
                                var reqSql = BuildRequest(hintCondition.Table, dialogueId.ToString(),
                                    hintCondition.Condition,
                                    hintCondition.Indexes);
                                var data = _repository.ExecuteDbCommand(hintCondition.Indexes, reqSql)
                                    .ToList();
                                switch (hintCondition.Operation)
                                {
                                    case "sum":
                                        if (!data.Any())
                                            resValue = 0;                                     
                                        else
                                        {
                                            foreach (var index in hintCondition.Indexes)
                                            {
                                                resValue += data.Sum(item =>
                                                    {                                                       
                                                        var property = item.GetType().GetProperty(index);                                                        
                                                        if (property != null)
                                                            return Double.Parse(property.GetValue(item).ToString());
                                                       
                                                        return 0;                                                        
                                                    });
                                            }
                                        }    
                                        if ((resValue >= hintCondition.Min) && (resValue <= hintCondition.Max))
                                        {   
                                            var textInfo = hintConditions.HintText.Where(p => p.Language == language)
                                                .ToList();
                                            if (textInfo.Any())
                                            {
                                                var dialogueHint = new DialogueHint
                                                {
                                                    DialogueId = dialogueId,
                                                    HintText = textInfo.First().Text,
                                                    IsAutomatic = true,
                                                    IsPositive = hintCondition.IsPositive,
                                                    Type = hintCondition.Type
                                                };                                                
                                                _context.DialogueHints.Add(dialogueHint);
                                            }
                                        }

                                        break;

                                    case "sub":
                                        if (data.Count() != 2)
                                        {
                                            resValue = 0;
                                        }
                                        else
                                        {
                                            resValue = data.Sum(item =>
                                            {
                                                var firstProperty = item.GetType().GetProperty(hintCondition.Indexes[0]);
                                                var secondProperty = item.GetType().GetProperty(hintCondition.Indexes[1]);
                                                Double first = 0;
                                                Double second = 0;
                                                if (firstProperty != null)
                                                    first = Double.Parse(firstProperty.GetValue(item).ToString());

                                                if (secondProperty != null)
                                                    second = Double.Parse(secondProperty.GetValue(item).ToString());

                                                return first - second;
                                            });
                                        }
                                        if ((resValue >= hintCondition.Min) && (resValue <= hintCondition.Max))
                                        {
                                            var textInfo = hintConditions.HintText.Where(p => p.Language == language)
                                                .ToList();
                                            if (textInfo.Any())
                                            {
                                                var dialogueHint = new DialogueHint
                                                {
                                                    DialogueId = dialogueId,
                                                    HintText = textInfo.First().Text,
                                                    IsAutomatic = true,
                                                    IsPositive = hintCondition.IsPositive,
                                                    Type = hintCondition.Type
                                                };                                                
                                                _context.DialogueHints.Add(dialogueHint);
                                            }
                                        }
                                        break;
                                }                                
                            }

                            _log.Info($"Table: {hintCondition.Table}");
                            _log.Info($"Conditions: {JsonConvert.SerializeObject(hintCondition.Condition)}");
                            _log.Info($"Result value is {resValue.Value}");
                        }                     
                    }

                    _context.SaveChanges();
                    System.Console.WriteLine($"function end");
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                throw;
            }
        }
        public static String BuildRequest(String tableName, String dialogueId, IEnumerable<Condition> conditions,
            List<String> fields)
        {
            StringBuilder request;
            if (fields.Any())
            {
                request = new StringBuilder("SELECT");
                for (var i = 0; i < fields.Count(); i++)
                    if (i == fields.Count() - 1)
                        request.Append($" \"{fields[i]}\"");
                    else
                        request.Append($" \"{fields[i]}\",");
            }
            else
            {
                request = new StringBuilder("SELECT *");
            }

            request.Append($" FROM public.\"{tableName}\"");
            request.Append($" WHERE CAST(\"DialogueId\" as uuid) = CAST('{dialogueId}' as uuid) ");
            if(conditions == null)
                return request.ToString();
            else
            {
                return !conditions.Any()
                ? request.ToString()
                : conditions.Aggregate(request, (current, cond) => current.Append($"AND CAST(\"{cond.Field}\" as uuid) = CAST('{cond.Value}' as uuid) ")).ToString();
            }            
        }
    }
}