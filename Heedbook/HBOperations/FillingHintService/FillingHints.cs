using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FillingHintService.Model;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace FillingHintService
{
    public class FillingHints
    {
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;

        public FillingHints(IServiceScopeFactory factory,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _log = log;
        }

        public async Task Run(Guid dialogueId)
        {
            try
            {
                _log.Info("Function filling hints started.");
                var language = _repository.GetWithInclude<Dialogue>(item => item.DialogueId == dialogueId,
                        item => item.Language)
                    .Select(item => item.Language.LanguageName)
                    .First();
                var catalogueHints = await _repository.FindAllAsync<CatalogueHint>();

                var hints = JsonConvert.DeserializeObject<List<Hint>>(JsonConvert.SerializeObject(catalogueHints));

                foreach (var hintConditions in hints)
                {
                    foreach (var hintCondition in hintConditions.HintCondition)
                    {
                        var reqSql = BuildRequest(hintCondition.Table, dialogueId.ToString(), hintCondition.Condition,
                            hintCondition.Indexes);

                        var data = _repository.ExecuteDbCommand(Tables.TablesDictionary[hintCondition.Table], reqSql)
                            .ToList();

                        Double? resValue = 0;
                        switch (hintCondition.Operation)
                        {
                            case "sum":
                                if (!data.Any())
                                    resValue = 0;
                                else
                                    foreach (var index in hintCondition.Indexes)
                                        resValue += data.Sum(item =>
                                        {
                                            var property = item.GetType().GetProperty(index);
                                            if (property != null)
                                                return Double.Parse(property.GetValue(item).ToString());

                                            return 0;
                                        });

                                if ((resValue >= hintCondition.Min) & (resValue <= hintCondition.Max))
                                {
                                    var textInfo = hintConditions.HintText.Where(p => p.Language == language).ToList();
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
                                        _repository.Create(dialogueHint);
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
                                    ;
                                }

                                if ((resValue >= hintCondition.Min) & (resValue <= hintCondition.Max))
                                {
                                    var textInfo = hintConditions.HintText.Where(p => p.Language == language).ToList();
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
                                        _repository.Create(dialogueHint);
                                    }
                                }

                                break;
                        }

                        _log.Info($"Table: {hintCondition.Table}");
                        _log.Info($"Conditions: {JsonConvert.SerializeObject(hintCondition.Condition)}");
                        _log.Info($"Result value is {resValue.Value}");
                    }
                }

                _repository.Save();
                _log.Info("Function filling hints ended.");
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
            String request;
            if (fields.Any())
            {
                request = "SELECT";
                for (var i = 0; i < fields.Count(); i++)
                    if (i == fields.Count() - 1)
                        request += $" {fields[i]}";
                    else
                        request += $" {fields[i]},";
            }
            else
            {
                request = "SELECT *";
            }

            request += $" FROM dbo.{tableName}";
            request += $" WHERE CAST(DialogueId as uniqueidentifier) = CAST('{dialogueId}' as uniqueidentifier) ";
            return !conditions.Any()
                ? request
                : conditions.Aggregate(request, (current, cond) => current + $"AND {cond.Field} = {cond.Value}");
        }
    }
}