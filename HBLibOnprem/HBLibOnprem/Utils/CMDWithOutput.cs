using System;
using System.Diagnostics;

namespace HBLib.Utils {
    public class CMDWithOutput {
        string output = "";
        private void OutputHandler(object sender, DataReceivedEventArgs e) {
            //The data we want is in e.Data, you must be careful of null strings
            string strMessage = e.Data;
            if (output != null && strMessage != null && strMessage.Length > 0) {
                output += string.Concat(strMessage, "\n");
            }
        }

        public string runCMD(string path, string arguments) {
            using (var proc = new Process()) {
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
            string res = String.Copy(output);
            output = "";
            return res;
        }
    }
}