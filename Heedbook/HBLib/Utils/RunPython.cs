using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HBLib.Utils
{
    public static class RunPython
    {
        public static (string, string) Run(string pyFilePath, string workDir, string version = "2.7", string args = "", ElasticClient _log = null)
        {
            var error = "";
            var result = "";
            
            _log?.Info($"RunPython(\"{pyFilePath}\", \"{workDir}\", \"{args}\")");
            
            try
            {
                var psi = new ProcessStartInfo();
                psi.Arguments = $"{pyFilePath} \"{args}\"";
                psi.FileName = $"python";
                if ( !string.IsNullOrWhiteSpace(version) )
                    psi.FileName+=version;

                psi.WorkingDirectory = workDir;

                _log?.Info($"RunPython: WorkingDirectory: {psi.WorkingDirectory}");
                
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                
                using (var pyProc = Process.Start(psi))
                { 
                    if (pyProc != null)
                    {
                        result = pyProc.StandardOutput.ReadToEnd();
                        error = pyProc.StandardError.ReadToEnd();
                        
                        _log?.Info($"RunPython: result {result}");
                        if ( !String.IsNullOrEmpty(error.Trim()))
                            _log?.Error($"RunPython: result {result}");
                        
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