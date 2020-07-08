using hb_asr_service.Utils;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace hb_asr_service.QuartzJob
{
    public class AudioRecognizeJob : IJob
    {
        private readonly ConcurrentQueue<FileAudioDialogue> _globalQueue;
        private readonly STTSettings _sttSettings;
        private RecordsContext _context;
        private ElasticClientFactory _elasticClientFactory;
        private static string output = String.Empty;
        private SftpClient _sftpClient;
        private IServiceScopeFactory _factory;

        public AudioRecognizeJob(
            STTSettings settings,
            ConcurrentQueue<FileAudioDialogue> globalQueue,
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _globalQueue = globalQueue;
            _factory = factory;
            _sttSettings = settings;
            _elasticClientFactory = elasticClientFactory;
            var sttUrl = Environment.GetEnvironmentVariable("STTURL");
            _sttSettings.STTUrl = (sttUrl == null) ? "http://0.0.0.0:8118/stt" : sttUrl;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            System.Console.WriteLine($"Stt settings are {JsonConvert.SerializeObject(_sttSettings)}");
            System.Console.WriteLine("Scheduler started");
            using (var scope = _factory.CreateScope())
            {
                _sftpClient = scope.ServiceProvider.GetRequiredService<SftpClient>();
                var log = _elasticClientFactory.GetElasticClient();
                Console.WriteLine("Started recognize");
                log.Info("Function OnPremAudioRecognize started");
                try
                {
                    if (!_globalQueue.IsEmpty)
                    {
                        if (!_globalQueue.TryDequeue(out var fileAudioDialogue))
                        {
                            Console.WriteLine("Cannot dequeue item for some reason");
                            log.Info("Cannot dequeue item for some reason");
                            return;
                        }
                        try
                        {
                            var path = await _sftpClient.DownloadFromFtpToLocalDiskAsync(
                                $"dialogueaudios/{fileAudioDialogue.FileName}");

                            log.Info($"Audio file path is {path}");
                            System.Console.WriteLine($"Audio file path is {path}");
                            fileAudioDialogue = _context.FileAudioDialogues.Where(item => item.DialogueId == fileAudioDialogue.DialogueId).FirstOrDefault();
                            var audioDuration = fileAudioDialogue.Duration;
                            System.Console.WriteLine($"Url is {_sttSettings.STTUrl}");
                            var result = await RecognizeSttAsync(path, _sttSettings.STTUrl, (double) fileAudioDialogue.Duration);
                            var res = new List<WordRecognized>();
                            if (result.result.Any())
                            {
                                res = result.result.Select(p => new WordRecognized{
                                    Time = p.start,
                                    Duration = p.end - p.start,
                                    Word = p.word                                    
                                }).OrderBy(p => p.Time).ToList();
                            }
                            fileAudioDialogue.StatusId = 6;
                            fileAudioDialogue.STTResult = JsonConvert.SerializeObject(res);
                            _context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            log.Fatal("Exception occured {e.ToString()}");
                            if ((DateTime.Now - fileAudioDialogue.CreationTime).Hours > 2)
                            {
                                fileAudioDialogue.StatusId = 8;
                                _context.SaveChanges();
                            }
                            else
                            {
                                _globalQueue.Enqueue(fileAudioDialogue);
                            }
                        }

                        File.Delete("/opt/download/" + fileAudioDialogue.FileName);
                    }
                    else
                    {
                        Console.WriteLine("Queue is empty. Nothing to recognize");
                        log.Info("Queue is empty. Nothing to recognize");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    log.Fatal(e.ToString());
                }
                Console.WriteLine("Audio recognize ended");
                log.Info("Function OnPremAudioRecognize ended");
            }
        }

        private async Task<RecognitionResult> RecognizeSttAsync(String path, String url, double duration)
        {
            System.Console.WriteLine(url);
            var client = new HttpClient();
            //client.Timeout = TimeSpan.FromSeconds(1.5 * duration);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1000, 1.5 * duration));
            System.Console.WriteLine($"Timeout is {client.Timeout} seconds");
            var basePath = path;

            var content = new MultipartFormDataContent();
            content.Add(
                new StringContent(basePath), "Path"
            );

            System.Console.WriteLine($"Send request to url {url}");
            System.Console.WriteLine($"Path is {basePath}");

            var result = await client.PostAsync(url, content);
            var contentResult = await result.Content.ReadAsStringAsync();
            System.Console.WriteLine(contentResult);
            return JsonConvert.DeserializeObject<RecognitionResult>(contentResult);
        }
    }
}