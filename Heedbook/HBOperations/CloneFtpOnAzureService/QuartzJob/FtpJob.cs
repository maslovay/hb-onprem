using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Quartz;
using Microsoft.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace CloneFtpOnAzureService
{
    public class FtpJob : IJob
    {
        private SftpClient _sftpClient;
        private BlobSettings _blobSettings;
        private SftpSettings _sftpSetting;
        private BlobClient _blobClient;
        private readonly ElasticClientFactory _elasticClientFactory;        
        private RecordsContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public FtpJob(IServiceScopeFactory scopeFactory,
            SftpClient sftpClient,
            BlobSettings blobSettings,
            BlobClient blobClient,
            ElasticClientFactory elasticClientFactory,
            SftpSettings sftpSetting)
        {
            _scopeFactory = scopeFactory;
            _elasticClientFactory = elasticClientFactory;
            _blobSettings = blobSettings;
            _blobClient = blobClient;
            _sftpClient = sftpClient;
            _sftpSetting = sftpSetting;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _log = _elasticClientFactory.GetElasticClient();
                try
                {
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();

                    var dialogues = _context.Dialogues
                        .Where(d => d.Status.StatusId == 3 
                            // && d.CreationTime >= DateTime.UtcNow.AddHours(-24))
                            && d.CreationTime >= new DateTime(2020, 02, 27, 0, 0, 1))
                        .OrderBy(p => p.CreationTime)
                        .Select(s => s.DialogueId)
                        .ToList();
                    var tasks = new List<Task>();
                    var dict = new Dictionary<String, String>()
                    {
                        //{_blobSettings.AvatarName, ".jpg"},
                        {_blobSettings.VideoName, ".mkv"},
                        {_blobSettings.AudioName, ".wav"}
                    };
                    System.Console.WriteLine("Try to download and upload");
                    System.Console.WriteLine($"dialogues count: {dialogues.Count}");
                    var counter = 0;
                    foreach (var dialogue in dialogues)
                    {   
                        counter++;                     
                        foreach (var (key, value) in dict)
                        {
                            var fileName = dialogue + value;
                            var filePath = key + "/" + fileName;
                            var thisFileExist = await _sftpClient.IsFileExistsAsync(filePath);
                            System.Console.WriteLine($"{counter}:{dialogues.Count} {fileName} {thisFileExist}");
                            if(thisFileExist)
                            {
                                var stream =  await _sftpClient.DownloadFromFtpAsMemoryStreamAsync(filePath);
                                tasks.Add(_blobClient.UploadFileStreamToBlob(key, fileName, stream));
                                _log.Info($"{fileName} sended on blobstorage");
                            }                                                   
                        }
                    }
                    await Task.WhenAll(tasks);

                    //ClientAvatars backup
                    var clientAvatars = await _sftpClient.GetAllFilesData(_sftpSetting.DestinationPath, _blobSettings.AvatarName);    
                                    
                    // var clientAvatarsForLastDay = clientAvatars.Where(p => p.date >= DateTime.Now.AddHours(-24))
                        // .OrderBy(p => p.date)                        
                        // .ToList();
                    var clientAvatarsForLastDay = clientAvatars.Where(p => p.date >= new DateTime(2020, 02, 27, 0, 0, 1))
                        .OrderBy(p => p.date)
                        .ToList();

                    System.Console.WriteLine($"clientavatars count: {clientAvatarsForLastDay.Count}");
                    foreach(var image in clientAvatarsForLastDay)
                    {
                        System.Console.WriteLine(image.name);
                        var stream =  await _sftpClient.DownloadFromFtpAsMemoryStreamAsync(image.url);
                        tasks.Add(_blobClient.UploadFileStreamToBlob(_blobSettings.AvatarName, image.name, stream));
                        _log.Info($"{image.name} sended on blobstorage");
                    }
                    await Task.WhenAll(tasks);

                    System.Console.WriteLine("Download and Upload finished");
                    _log.Info($"Downloaded and Uploaded {dialogues.Count} dialogues data");
                }
                catch (Exception e)
                {
                    _log.Fatal($"{e}");
                }
            }
        }
    }
}