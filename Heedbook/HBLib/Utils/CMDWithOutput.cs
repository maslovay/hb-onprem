using System;
using System.Diagnostics;
using MongoDB.Driver.Core.Authentication.Sspi;

namespace HBLib.Utils
{
    public class CMDWithOutput
    {
        private String output = "";

        private void OutputHandler(Object sender, DataReceivedEventArgs e)
        {
            //The data we want is in e.Data, you must be careful of null strings
            var strMessage = e.Data;
            if (output != null && strMessage != null && strMessage.Length > 0)
                output += String.Concat(strMessage, "\n");
        }

        public String runCMD(String path, String arguments)
        {
            try
            {
                using (var proc = new Process())
                {
                    proc.StartInfo.Arguments = arguments;
                    proc.StartInfo.FileName = path;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.ErrorDataReceived += OutputHandler;
                    proc.OutputDataReceived += OutputHandler;
                    proc.EnableRaisingEvents = true;
                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    proc.WaitForExit();
                }

                var res = String.Copy(output);
                output = "";
                return res;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} \r\n executable: {path}"); // for tests!
            }

        }
    }
}