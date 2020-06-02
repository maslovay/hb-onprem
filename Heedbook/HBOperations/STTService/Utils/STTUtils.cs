using System;
using System.IO;
using HBLib.Utils;

namespace hb_asr_service.Utils
{
    public class STTUtils
    {
        public STTUtils()
        {
        }

        public string Execute(string path)
        {
            var websocketUrl = Environment.GetEnvironmentVariable("WEBSOCKET");
            if (websocketUrl == null) websocketUrl = "ws://localhost:2700";
            var cmd = new CMDWithOutput();

            var args = $"stt.py {path} {websocketUrl}";
            System.Console.WriteLine(args);
            var output = cmd.runCMD("python3", args);
            System.Console.WriteLine(output);
            return output;
        }
    }

}