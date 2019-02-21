using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzeService
{
    public class ToneAnalyze
    {
        private readonly SftpClient _sftpClient;

        private readonly IConfiguration _configuration;

        private readonly IGenericRepository _repository;

        public ToneAnalyze(SftpClient sftpClient,
            IConfiguration configuration,
            IServiceProvider provider)
        {
            _sftpClient = sftpClient;
            _configuration = configuration;
            _repository = provider.GetRequiredService<IGenericRepository>();
        }

        public async Task Run(String path)
        {
            Console.WriteLine("Function Tone analyze started");
            var ffmpeg = new FFMpegWrapper(_configuration["FfmpegPath"]);
            var dialogueId = Guid.Parse((ReadOnlySpan<char>) Path.GetFileNameWithoutExtension(path.Split('/').Last()));
            var seconds = 3;
            var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
            var metadata = ffmpeg.SplitBySeconds(localPath, seconds);
            var intervals = new List<DialogueInterval>();
            var begTime = _repository.Get<Dialogue>().Where(item => item.DialogueId == dialogueId)
                .Select(item => item.BegTime).First();
            foreach (var currentMetadata in metadata)
            {
                var fileName = currentMetadata["fn"];
                try
                {
                    var result = RecognizeTone(_configuration["VacaturiPath"], fileName);
                    var beginTime = begTime;
                    var endTime = beginTime.AddSeconds(seconds);
                    intervals.Add(new DialogueInterval
                    {
                        DialogueId = dialogueId,
                        IsClient = true,
                        BegTime = beginTime,
                        EndTime = endTime,
                        AngerTone = result["Anger"],
                        FearTone = result["Fear"],
                        HappinessTone = result["Happiness"],
                        NeutralityTone = result["Neutrality"],
                        SadnessTone = result["Sadness"]
                    });
                    begTime = endTime;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            var dialogueAudio = new DialogueAudio
            {
                IsClient = true,
                DialogueId = dialogueId,
                NegativeTone = intervals.Average(item => item.AngerTone + item.SadnessTone + item.FearTone),
                PositiveTone = intervals.Average(item => item.HappinessTone),
                NeutralityTone = intervals.Average(item => item.NeutralityTone)
            };
            await _repository.BulkInsertAsync(intervals);
            await _repository.CreateAsync(dialogueAudio);
            await _repository.SaveAsync();
            OS.SafeDelete(localPath);
            Console.WriteLine("Function Tone analyze finished");
        }

        public static Dictionary<string, double> RecognizeTone(String vokaturi, string fileName)
        {
            /***********
            WAV files analyzed with:
            OpenVokaturi version 2.1 for open-source projects, 2017-01-13
            Distributed under the GNU General Public License, version 3 or later
            **********

            Emotion analysis of WAV file .\sample.wav:
            Neutrality 0.614982
            Happiness 0.000023
            Sadness 0.174298
            Anger 0.001639
            Fear 0.209058*/
            var cmd = new CMDWithOutput();
            var text = cmd.runCMD(vacaturiPath, fileName);
            try
            {
                var pattern = @"\n\s?(\w+)\s+([\d\.]+)";

                /*{
                  "Neutrality": 0.614982,
                  "Happiness": 2.3E-05,
                  "Sadness": 0.174298,
                  "Anger": 0.001639,
                  "Fear": 0.209058
                }*/
                var result = new Dictionary<string, double>();

                var matches = Regex.Matches(text, pattern);
                foreach (Match match in matches)
                {
                    var emotion = match.Groups[1].ToString();
                    var value = match.Groups[2].ToString();
                    result[emotion] = double.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
                return result;
            }
            catch
            {
                throw new Exception($"Something went wrong! The error message: {text}");
            }
        }
    }
}