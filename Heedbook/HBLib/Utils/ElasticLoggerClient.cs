using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Network;

namespace HBLib.Utils

{
    public interface IElasticLogger
    {
        void Debug(String s, params Object[] args);
        void Info(String s, params Object[] args);
        void Warning(String s, params Object[] args);
        void Error(String s, params Object[] args);
        void Fatal(String s, params Object[] args);
    }


    public class ElasticClient : IElasticLogger
    {
        private readonly ElasticSettings _elasticSettings;
        private readonly String _invocationId;
        private readonly Logger _logger;
        private Object[] _args;
        private String _format;


        public ElasticClient(ElasticSettings elasticSettings, String format, params Object[] args)
        {
            _elasticSettings = elasticSettings;
            _logger = new LoggerConfiguration()
                     .WriteTo.TCPSink(IPAddress.Parse(elasticSettings.Host), elasticSettings.Port).CreateLogger();
            
            _invocationId = Guid.NewGuid().ToString();
            _format = format + ": ";
            _args = args;
            LogstashLog($"Function started: {elasticSettings.FunctionName}");
        }

        public ElasticClient(ElasticSettings elasticSettings)
        {
            _elasticSettings = elasticSettings;
            _logger = new LoggerConfiguration()
                     .WriteTo.TCPSink(IPAddress.Parse(elasticSettings.Host), elasticSettings.Port).CreateLogger();
            _invocationId = Guid.NewGuid().ToString();
            _format = "";
            _args = Array.Empty<Object>();
            LogstashLog($"Function started: {elasticSettings.FunctionName}");
        }

        public void Debug(String s, params Object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Debug(doc);
        }

        public void Info(String s, params Object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Information(doc);
        }

        public void Warning(String s, params Object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Warning(doc);
        }

        public void Error(String s, params Object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Error(doc);
        }

        public void Fatal(String s, params Object[] args)
        {
            var doc = JsonConvert.SerializeObject(GetJson(s, _args.Concat(args).ToArray()));
            _logger.Fatal(doc);
        }

        public void SetFormat(String format)
        {
            _format = format;
        }

        public void SetArgs(params Object[] args)
        {
            _args = args;
        }

        public Dictionary<String, String> GetJson(String s, params Object[] args)
        {
            var js = new Dictionary<String, String>();

            var resArgs = _args.Concat(args).ToList();
            var resS = $"{_format}{s}";

            js["OriginalFormat"] = resS;
            if (_elasticSettings.FunctionName != null)
            {
                js["FunctionName"] = _elasticSettings.FunctionName;
                js["InvocationId"] = _invocationId;
            }

            var pattern = @"{([^}:]+)}";
            var matches = Regex.Matches(resS, pattern);

            var stringArgs = new List<String>();

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i].Groups[1];
                stringArgs.Add(match.Value);
            }

            var m = Math.Min(resArgs.Count(), stringArgs.Count());

            var customDimensions = new Dictionary<String, String>();
            for (var i = 0; i < m; i++) customDimensions[$"prop__{stringArgs[i]}"] = resArgs[i].ToString();
            js["customDimensions"] = JsonConvert.SerializeObject(customDimensions);
            return js;
        }

        public void LogstashLog(String message)
        {
            var doc = JsonConvert.SerializeObject(GetJson(message));
            _logger.Information(doc);
        }
    }
}