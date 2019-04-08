using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            var dialogueId = Guid.Parse((ReadOnlySpan<char>)Path.GetFileNameWithoutExtension(path.Split('/').Last()));
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
                    Console.WriteLine(fileName);
                    Console.WriteLine(_configuration["VokaturiPath"]);
                    var result = RecognizeTone(_configuration["VokaturiPath"], fileName);
                    var beginTime = begTime;
                    var endTime = beginTime.AddSeconds(seconds);
                    intervals.Add(new DialogueInterval
                    {
                        DialogueId = dialogueId,
                        IsClient = true,
                        BegTime = beginTime,
                        EndTime = endTime,
                        AngerTone = result.TryGetValue("Anger", out var anger) ? anger : default(double?),
                        FearTone = result.TryGetValue("Fear", out var fear) ? fear : default(double?),
                        HappinessTone = result.TryGetValue("Happiness", out var happiness) ? happiness : default(double?),
                        NeutralityTone = result.TryGetValue("Neutrality", out var neutrality) ? neutrality : default(double?),
                        SadnessTone = result.TryGetValue("Sadness", out var sadness) ? sadness : default(double?)
                    });
                    begTime = endTime;
                    File.Delete(fileName);
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

        private static void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            //The data we want is in e.Data, you must be careful of null strings
            var strMessage = e.Data;
            if (output != null && strMessage != null && strMessage.Length > 0)
                output += string.Concat(strMessage, "\n");
        }
        private static string output = "";

        public static Dictionary<string, double> RecognizeTone(String vokaturiPath, string fileName)
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
            var psi = new ProcessStartInfo("wine64")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"{vokaturiPath} {fileName}"
            };
            using (var proc = new Process { StartInfo = psi })
            {
                proc.EnableRaisingEvents = true;
                proc.OutputDataReceived += OutputHandler;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
            }

            var text = output;
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
                Console.WriteLine(text);
                output = "";
                return result;
            }
            catch
            {
                throw new Exception($"Something went wrong! The error message: {text}");
            }
        }
    }
}