using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using HBData;
using HBData.Models;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceBus.Messaging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class FillingSubHints
    {
        [FunctionName("Filling_Sub_Hints")]
        public static void Run(
            string mySbMsg, ExecutionContext dir, ILogger log)
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
                var language = HeedbookMessengerStatic.Context().Dialogues
                                                      .Include(p => p.Language)
                                                      .First(p => p.DialogueId.ToString() == dialogueId).Language
                                                      .LanguageName;

                log.LogInformation($"{language}");
                var collectionFrame =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionDialogueHints"));
                var docs = BsonSerializer.Deserialize<List<MongoHint>>(collectionFrame.Find(new BsonDocument { })
                                                                                      .ToList().ToJson());
                foreach (var hintConditions in docs)
                {
                    foreach (var hintCondition in hintConditions.hintConditions)
                    {
                        var reqSql = BuildRequest(hintCondition.table, dialogueId, hintCondition.condition,
                            hintCondition.indexes);

                        var dynamicData = DynamicListFromSql(HeedbookMessengerStatic.Context(), reqSql).ToList();
                        var data = JsonConvert.DeserializeObject<List<Dictionary<string, double?>>>(
                            JsonConvert.SerializeObject(dynamicData));

                        double? resValue = 0;
                        switch (hintCondition.operation)
                        {
                            case "sum":
                                if (data.Count() == 0)
                                {
                                    resValue = 0;
                                }
                                else
                                {
                                    foreach (var index in hintCondition.indexes)
                                    {
                                        resValue += data[0][index];
                                    }
                                }

                                log.LogInformation($"{resValue}");

                                if (resValue >= hintCondition.min & resValue <= hintCondition.max)
                                {
                                    var textInfo = hintConditions.hintText.Where(p => p.language == language);
                                    if (textInfo.Count() > 0)
                                    {
                                        log.LogInformation(
                                            $"Add hint {textInfo.First().text} to dialogue {dialogueId}");
                                        var dialogueHint = new DialogueHint();
                                        dialogueHint.DialogueId = Guid.Parse(dialogueId);
                                        dialogueHint.HintText = textInfo.First().text;
                                        dialogueHint.IsAutomatic = true;
                                        dialogueHint.IsPositive = hintCondition.isPositive;
                                        dialogueHint.Type = hintCondition.type;

                                        HeedbookMessengerStatic.Context().DialogueHints.Add(dialogueHint);
                                        HeedbookMessengerStatic.Context().SaveChanges();
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
                                    resValue = data[0][hintCondition.indexes[0]] - data[0][hintCondition.indexes[1]];
                                }

                                log.LogInformation($"{resValue}");

                                if (resValue >= hintCondition.min & resValue <= hintCondition.max)
                                {
                                    var textInfo = hintConditions.hintText.Where(p => p.language == language);
                                    if (textInfo.Count() > 0)
                                    {
                                        log.LogInformation(
                                            $"Add hint {textInfo.First().text} to dialogue {dialogueId}");
                                        var dialogueHint = new DialogueHint();
                                        dialogueHint.DialogueId = Guid.Parse(dialogueId);
                                        dialogueHint.HintText = textInfo.First().text;
                                        dialogueHint.IsAutomatic = true;
                                        dialogueHint.IsPositive = hintCondition.isPositive;
                                        dialogueHint.Type = hintCondition.type;

                                        HeedbookMessengerStatic.Context().DialogueHints.Add(dialogueHint);
                                        HeedbookMessengerStatic.Context().SaveChanges();
                                    }
                                }

                                break;

                            default:
                                break;
                        }
                    }
                }

                log.LogInformation($"Function finished {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw e;
            }
        }

        public static string BuildRequest(string tableName, string dialogueId, List<Condition> conditions,
            List<string> fields)
        {
            string request;
            if (fields.Count() > 0)
            {
                request = "SELECT";
                for (int i = 0; i < fields.Count(); i++)
                {
                    if (i == fields.Count() - 1)
                        request += $" {fields[i]}";
                    else
                        request += $" {fields[i]},";
                }
            }
            else
            {
                request = "SELECT *";
            }

            request += $" FROM dbo.{tableName}";
            request += $" WHERE CAST(DialogueId as uniqueidentifier) = CAST('{dialogueId}' as uniqueidentifier) ";
            if (conditions.Count() > 0)
            {
                foreach (var cond in conditions)
                {
                    request += $"AND {cond.field} = {cond.value}";
                }
            }

            return request;
        }


        public class HintCondition
        {
            public List<string> indexes;
            public List<Condition> condition;
            public string table;
            public string type;
            public bool isPositive;
            public string operation;
            public double min;
            public double max;
        }

        public class Condition
        {
            public string field;
            public double? value;
        }

        public class HintText
        {
            public string text;
            public string language;
        }

        public class MongoHint
        {
            public ObjectId _id;
            public List<HintCondition> hintConditions;
            public List<HintText> hintText;
        }

        public class Test
        {
            public double? HappinessShare { get; set; }
            public double? SurpriseShare { get; set; }
        }


        public static IEnumerable<dynamic> DynamicListFromSql(this RecordsContext db, string Sql,
            Dictionary<string, object> Params = null)
        {
            using (var cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = Sql;
                if (cmd.Connection.State != ConnectionState.Open)
                {
                    cmd.Connection.Open();
                }

                if (Params != null)
                {
                    foreach (KeyValuePair<string, object> p in Params)
                    {
                        DbParameter dbParameter = cmd.CreateParameter();
                        dbParameter.ParameterName = p.Key;
                        dbParameter.Value = p.Value;
                        cmd.Parameters.Add(dbParameter);
                    }
                }

                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var row = new ExpandoObject() as IDictionary<string, object>;
                        for (var fieldCount = 0; fieldCount < dataReader.FieldCount; fieldCount++)
                        {
                            row.Add(dataReader.GetName(fieldCount), dataReader[fieldCount]);
                        }

                        yield return row;
                    }
                }
            }
        }
    }
}