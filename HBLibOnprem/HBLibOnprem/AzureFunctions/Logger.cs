/*using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace HBLib.AzureFunctions {
    public interface IAzureLogger {
        void Debug(string s, params object[] args);
        void Info(string s, params object[] args);
        void Warning(string s, params object[] args);
        void Error(string s, params object[] args);
        void Critical(string s, params object[] args);
    }

    public static class ILoggerExtensions {
        public static void Debug(this ILogger log, string s, params object[] args) {
            log.LogDebug(s, args);
        }
        public static void Info(this ILogger log, string s, params object[] args) {
            log.LogInformation(s, args);
        }
        public static void Warning(this ILogger log, string s, params object[] args) {
            log.LogWarning(s, args);
        }
        public static void Error(this ILogger log, string s, params object[] args) {
            log.LogError(s, args);
        }
        public static void Critical(this ILogger log, string s, params object[] args) {
            log.LogCritical(s, args);
        }
    }

    class ILoggerAdapter : IAzureLogger {
        public ILogger log;
        public object[] args;
        public string format;
        public ILoggerAdapter(ILogger log, ExecutionContext dir, string format, params object[] args) {
            this.log = log;
            this.format = format + ": ";
            this.args = args;
        }

        public ILoggerAdapter(ILogger log, ExecutionContext dir) {
            this.log = log;
            this.args = Array.Empty<object>();
            this.format = "";
        }

        public void Debug(string s, params object[] args) {
            this.log.Debug($"{this.format}{s}", this.args.Concat(args).ToArray());
        }
        public void Info(string s, params object[] args) {
            this.log.Info($"{this.format}{s}", this.args.Concat(args).ToArray());
        }
        public void Warning(string s, params object[] args) {
            this.log.Warning($"{this.format}{s}", this.args.Concat(args).ToArray());
        }
        public void Error(string s, params object[] args) {
            this.log.Error($"{this.format}{s}", this.args.Concat(args).ToArray());
        }
        public void Critical(string s, params object[] args) {
            this.log.Critical($"{this.format}{s}", this.args.Concat(args).ToArray());
        }
    }

    class ILoggerAdapterSB : IAzureLogger {
        public ILogger log;
        public object[] args;
        public string format;
        public HeedbookMessenger messenger;
        public IMongoCollection<BsonDocument> collection;
        public ExecutionContext dir;

        public ILoggerAdapterSB(ILogger log, ExecutionContext dir, string format, params object[] args) {
            this.log = log;
            this.format = format + ": ";
            this.dir = dir;
            this.args = args;

            this.messenger = new HeedbookMessenger();
            var client = messenger.mongoDB;
            this.collection = client.db.GetCollection<BsonDocument>(EnvVar.Get("MongoDBLog"));

            this.MongoLog($"Function started: {dir.FunctionName}");
        }

        public ILoggerAdapterSB(ILogger log, ExecutionContext dir) {
            this.log = log;
            this.format = "";
            this.dir = dir;
            this.args = Array.Empty<object>();

            this.messenger = new HeedbookMessenger();
            var client = messenger.mongoDB;
            this.collection = client.db.GetCollection<BsonDocument>(EnvVar.Get("MongoDBLog"));
            
            this.MongoLog($"Function started: {dir.FunctionName}");
        }

        public void SafeInsert(IMongoCollection<BsonDocument> collection, BsonDocument doc, int N=5) {
            messenger.mongoDB.SafeInsert(collection, doc);
        }

        public void MongoLog(string message) {
                var doc = new BsonDocument(GetJson("Info", message));
                SafeInsert(this.collection, doc);
        }
        public void MongoLog(string logLevel, string s, params object[] args) {
            var doc = new BsonDocument(GetJson(logLevel, s, args));
            SafeInsert(this.collection, doc);
        }

        public Dictionary<string, string> GetJson(string logLevel, string s, params object[] args) {
            var js = new Dictionary<string, string>();
            js["LogLevel"] = logLevel;
            js["SharedCode"] = EnvVar.Get("MongoDBSharedCode");

            var resArgs = this.args.Concat(args).ToList();
            var resS = $"{this.format}{s}";
            
            js["OriginalFormat"] = resS;
            if (this.dir != null) {
                js["FunctionName"] = this.dir.FunctionName;
                js["InvocationId"] = this.dir.InvocationId.ToString();
            }
            // {var1} {var2} -> ["var1", "var2"]
            string pattern = @"{([^}:]+)}";
            var matches = Regex.Matches(resS, pattern);

            var stringArgs = new List<string>();
            
            for (int i = 0; i < matches.Count; i++) {
                var match = matches[i].Groups[1];
                stringArgs.Add(match.Value);
            }

            int m = Math.Min(resArgs.Count(), stringArgs.Count());

            var customDimensions = new Dictionary<string, string>();

            for (int i = 0; i < m; i++) {
                customDimensions[$"prop__{stringArgs[i]}"] = resArgs[i].ToString();
            }

            js["customDimensions"] = customDimensions.JsonPrint();

            js["LogTime"] = DT.Format(DateTime.Now, isSystem:false);
            
            return js;
        }        

        public void Debug(string s, params object[] args) {
            this.log.Debug($"{this.format}{s}", this.args.Concat(args).ToArray());
            //this.MongoLog("Debug", s, args);
        }
        public void Info(string s, params object[] args) {
            this.log.Info($"{this.format}{s}", this.args.Concat(args).ToArray());
            this.MongoLog("Info", s, args);
        }
        public void Warning(string s, params object[] args) {
            this.log.Warning($"{this.format}{s}", this.args.Concat(args).ToArray());
            this.MongoLog("Warning", s, args);
        }
        public void Error(string s, params object[] args) {
            this.log.Error($"{this.format}{s}", this.args.Concat(args).ToArray());
            this.MongoLog("Error", s, args);
        }
        public void Critical(string s, params object[] args) {
            this.log.Critical($"{this.format}{s}", this.args.Concat(args).ToArray());
            this.MongoLog("Critical", s, args);
        }
    }

    public static class TraceWriterExtensions {
        public static void Debug(this TraceWriter log, string s, params object[] args) {
            log.Info("Debug message. " + s);
        }
        public static void Info(this TraceWriter log, string s, params object[] args) {
            log.Info(s);
        }
        public static void Warning(this TraceWriter log, string s, params object[] args) {
            log.Warning(s);
        }
        public static void Error(this TraceWriter log, string s, params object[] args) {
            log.Error(s);
        }
        public static void Critical(this TraceWriter log, string s, params object[] args) {
            log.Error("Critical error. " + s);
        }
    }


    class SimpleTraceWriterAdapter : IAzureLogger {
        public TraceWriter log;

        public SimpleTraceWriterAdapter(TraceWriter log, ExecutionContext dir) {
            this.log = log;
        }

        public SimpleTraceWriterAdapter(TraceWriter log, ExecutionContext dir, string format, params object[] args) {
            this.log = log;
        }

        public void Debug(string s, params object[] args) {
            this.log.Debug(s);
        }

        public void Info(string s, params object[] args) {
            this.log.Info(s);
        }

        public void Warning(string s, params object[] args) {
            this.log.Warning(s);
        }

        public void Error(string s, params object[] args) {
            this.log.Error(s);
        }
        public void Critical(string s, params object[] args) {
            this.log.Critical(s);
        }
    }


    class TraceWriterAdapter : IAzureLogger {
        public TraceWriter log;
        public object[] args;
        public string format;

        public TraceWriterAdapter(TraceWriter log, ExecutionContext dir) {
            this.log = log;
            this.args = Array.Empty<object>();
            this.format = "";
        }

        public TraceWriterAdapter(TraceWriter log, ExecutionContext dir, string format, params object[] args) {
            this.log = log;
            this.args = args;
            this.format = format + ": ";
        }

        public string MakeFormat(string fmt) {
            // {var1} {var2} -> {0} {1}
            string pattern = @"{([^}:]+)}";
            var matches = Regex.Matches(fmt, pattern);
            int count = 0;
            for (int i = 0; i < matches.Count; i++) {
                var m = matches[i].Groups[1];
                fmt = fmt.Remove(m.Index - count, m.Length).Insert(m.Index - count, i.ToString());
                count += m.Length - i.ToString().Length;
            }
            return fmt;
        }

        public string SafeLogMsg(string s, params object[] args) {
            try {
                return String.Format(MakeFormat($"{this.format}{s}"), this.args.Concat(args).ToArray());
            } catch (Exception e){
                return s;
            }
        }

        public void Debug(string s, params object[] args) {
            this.log.Debug(SafeLogMsg(s, args));
        }

        public void Info(string s, params object[] args) {
            this.log.Info(SafeLogMsg(s, args));
        }

        public void Warning(string s, params object[] args) {
            this.log.Warning(SafeLogMsg(s, args));
        }

        public void Error(string s, params object[] args) {
            this.log.Error(SafeLogMsg(s, args));
        }
        public void Critical(string s, params object[] args) {
            this.log.Critical(SafeLogMsg(s, args));
        }
    }


    public class AzureLogger : IAzureLogger {
        IAzureLogger log;
        public AzureLogger(IAzureLogger log) {
            this.log = log;
        }
        public void Debug(string s, params object[] args) {
            this.log.Debug(s, args);
        }
        public void Info(string s, params object[] args) {
            this.log.Info(s, args);
        }
        public void Warning(string s, params object[] args) {
            this.log.Warning(s, args);
        }
        public void Error(string s, params object[] args) {
            this.log.Error(s, args);
        }
        public void Critical(string s, params object[] args) {
            this.log.Critical(s, args);
        }
    }


    public static class LoggerFactory {
        public static IAzureLogger CreateAdapter(object log, ExecutionContext dir) {
            if (log is ILogger) {
                return new ILoggerAdapterSB((ILogger)log, dir);
            } else {
                return new TraceWriterAdapter((TraceWriter)log, dir);
            }
        }

        public static IAzureLogger CreateAdapter(object log, ExecutionContext dir, string format, params object[] args) {
            if (log is ILogger) {
                return new ILoggerAdapterSB((ILogger)log, dir, format, args);
            } else {
                return new TraceWriterAdapter((TraceWriter)log, dir, format, args);
            }
        }
    }
}*/
