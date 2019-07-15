using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Renci.SshNet.Common;

namespace ToneAnalyzeService
{
    public class ToneAnalyze
    {
        private static String output = "";
        private readonly IConfiguration _configuration;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private readonly SftpClient _sftpClient;
        private readonly FFMpegWrapper _wrapper;
        private readonly ElasticClientFactory _elasticClientFactory;


        public ToneAnalyze(SftpClient sftpClient,
            IConfiguration configuration,
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            FFMpegWrapper wrapper)
        {
            _sftpClient = sftpClient;
            _configuration = configuration;
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _elasticClientFactory = elasticClientFactory;
            _wrapper = wrapper;
        }


        public async Task Run(String path)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(path);
            try
            {
                _log.Info("Function started");
                var dialogueId =
                    Guid.Parse((ReadOnlySpan<Char>) Path.GetFileNameWithoutExtension(path.Split('/').Last()));
                var seconds = 3;
                var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
                var metadata = _wrapper.SplitBySeconds(localPath, seconds);
                var intervals = new List<DialogueInterval>();
                var begTime = _repository.Get<Dialogue>().Where(item => item.DialogueId == dialogueId)
                                         .Select(item => item.BegTime).First();
                foreach (var currentMetadata in metadata)
                {
                    var fileName = currentMetadata["fn"];

                    _log.Info(fileName);
                    var result = RecognizeTone(_configuration["VokaturiPath"], fileName, _log);
                    var beginTime = begTime;
                    var endTime = beginTime.AddSeconds(seconds);
                    intervals.Add(new DialogueInterval
                    {
                        DialogueId = dialogueId,
                        IsClient = true,
                        BegTime = beginTime,
                        EndTime = endTime,
                        AngerTone = result.TryGetValue("Anger", out var anger) ? anger : default(Double?),
                        FearTone = result.TryGetValue("Fear", out var fear) ? fear : default(Double?),
                        HappinessTone = result.TryGetValue("Happiness", out var happiness)
                            ? happiness
                            : default(Double?),
                        NeutralityTone = result.TryGetValue("Neutrality", out var neutrality)
                            ? neutrality
                            : default(Double?),
                        SadnessTone = result.TryGetValue("Sadness", out var sadness) ? sadness : default(Double?)
                    });
                    begTime = endTime;
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
                File.Delete(localPath);
                _log.Info("Function Tone analyze finished");
            }
            catch (SftpPathNotFoundException e)
            {
                _log.Fatal($"{e}");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurs {e}");
                throw;
            }
        }

        private static void OutputHandler(Object sender, DataReceivedEventArgs e)
        {
            //The data we want is in e.Data, you must be careful of null strings
            var strMessage = e.Data;
            if (output != null && strMessage != null && strMessage.Length > 0)
                output += String.Concat(strMessage, "\n");
        }

        private Dictionary<String, Double> RecognizeTone(String vokaturiPath, String fileName, ElasticClient _log)
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
            using (var proc = new Process {StartInfo = psi})
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
                var result = new Dictionary<String, Double>();

                var matches = Regex.Matches(text, pattern);
                foreach (Match match in matches)
                {
                    var emotion = match.Groups[1].ToString();
                    var value = match.Groups[2].ToString();
                    result[emotion] = Double.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }

                _log.Info(text);
                output = "";
                File.Delete(fileName);
                return result;
            }
            catch
            {
                _log.Fatal(text);
                File.Delete(fileName);
                throw new Exception($"Something went wrong! The error message: {text}");
            }
        }
    }
}