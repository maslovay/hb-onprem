using System;
using System.IO;
using System.Threading.Tasks;
using HBLib.Utils;
using Quartz;

namespace QuartzExtensions.Jobs
{
    public class DeleteOldFilesJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var files = OS.GetFiles("/opt/download", String.Empty, SearchOption.AllDirectories);
            var now = DateTime.Now;
            foreach (var file in files)
            {
                if ((now - File.GetLastWriteTime(file)).Hours <= 1) continue;
                OS.SafeDelete(file);
                Console.WriteLine(file + " deleted");
            }
        }
    }
}