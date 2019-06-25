using System;
using System.Diagnostics;
using System.IO;

namespace HBLib.Utils
{
    public static class RunPython
    {
        public static (string, string) Run(string pyFilePath, string workDir, string version = "2.7", string args = "")
        {
            var error = "";
            var result = "";
            
            try
            {
                var psi = new ProcessStartInfo();
                psi.Arguments = $"{pyFilePath} \"{args}\"";
                psi.FileName = $"python";
                if ( !string.IsNullOrWhiteSpace(version) )
                    psi.FileName+=version;

                psi.WorkingDirectory = workDir;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                
                using (var pyProc = Process.Start(psi))
                {
                    if (pyProc != null)
                    {
                        result = pyProc.StandardOutput.ReadToEnd();
                        error = pyProc.StandardError.ReadToEnd();
                        return (result, error);
                    }
                }
            }
            catch (Exception e)
            {
                return (string.Empty, e.Message);                
            }
            
            return (result, error);   
        }
    }
}