using System;
using System.IO;
using System.Linq;
using HBLib.AzureFunctions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace OperationService.Legacy
{
    public static class FileScheduleControl
    {
        [FunctionName("File_Schedule_Control")]
        public static void Run([TimerTrigger("0 */10 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext dir)
        {
            try
            {
                var path = Misc.GetTempPath();
                var data = "data";
                var dataPath = Path.Combine(path, data);

                var files = Directory.GetDirectories(dataPath);

                var dll = Misc.BinPath(dir);

                log.LogInformation($"Total files {files.Count()}");
                var filesDeleted = 0;
                foreach (var file in files)
                {
                    try
                    {
                        if (File.GetCreationTime(file) < DateTime.Now.AddMinutes(-15))
                        {
                            Directory.Delete(file, true);
                            filesDeleted += 1;
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Exception occured {e}");
                    }
                }
                log.LogError($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }
    }
}
