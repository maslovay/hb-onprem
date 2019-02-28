using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Network;


namespace HBLib.Utils

{
    public interface ElasticLogger
    {
        void Debug(string s, params object[] args);
        void Info(string s, params object[] args);
        void Warning(string s, params object[] args);
        void Error(string s, params object[] args);
        void Fatal(string s, params object[] args);
    }


    public class ElasticClient : ElasticLogger
    {
        private readonly Logger _logger;
        private readonly ElasticSettings _elasticSettings;
        private readonly object[] _args;
        private readonly string _format;
        private readonly string _invocationId;


        public ElasticClient(ElasticSettings elasticSettings, string format, params object[] args)
        {
            _elasticSettings = elasticSettings;
            _logger = new LoggerConfiguration().WriteTo.TCPSink(IPAddress.Parse(elasticSettings.Host), elasticSettings.Port).CreateLogger();
            _invocationId = Guid.NewGuid().ToString();
            _format = format + ": ";
            _args = args;
            this.LogstashLog($"Function started: {elasticSettings.FunctionName}");
        }

        public ElasticClient(ElasticSettings elasticSettings)
        {
            _elasticSettings = elasticSettings;
            _logger = new LoggerConfiguration().WriteTo.TCPSink(IPAddress.Parse(elasticSettings.Host), elasticSettings.Port).CreateLogger();
            _invocationId = Guid.NewGuid().ToString();
            _format = "";
            _args = Array.Empty<object>();
            this.LogstashLog($"Function started: {elasticSettings.FunctionName}");
        }


        public Dictionary<string, string> GetJson(string s, params object[] args )
        {
            var js = new Dictionary<string, string>();

            var resArgs = _args.Concat(args).ToList();
            var resS = $"{_format}{s}";

            js["OriginalFormat"] = resS;
            if (_elasticSettings.FunctionName != null)
            {
                js["FunctionName"] = _elasticSettings.FunctionName;
                js["InvocationId"] = _invocationId;
            }
            string pattern = @"{([^}:]+)}";
            var matches = Regex.Matches(resS, pattern);

            var stringArgs = new List<string>();

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i].Groups[1];
                stringArgs.Add(match.Value);
            }
            int m = Math.Min(resArgs.Count(), stringArgs.Count());

            var customDimensions = new Dictionary<string, string>();
            for (int i = 0; i < m; i++)
            {
                customDimensions[$"prop__{stringArgs[i]}"] = resArgs[i].ToString();
            }
            js["customDimensions"] = customDimensions.JsonPrint();
            return js;
        }

        public void LogstashLog(string message)
        {
            var doc = JsonConvert.SerializeObject(GetJson(message));
            Console.WriteLine($"new message in elastic {doc}");
            _logger.Information(doc);
        }

        public void Debug(string s, params object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Debug(doc);
        }
        public void Info(string s, params object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Information(doc);
        }
        public void Warning(string s, params object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Warning(doc);
        }
        public void Error(string s, params object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Error(doc);
        }
        public void Fatal(string s, params object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Fatal(doc);
        }
    }
}