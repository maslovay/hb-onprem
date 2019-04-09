using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace DialogueVideoMergeService
{
    public class DialogueVideoMerge
    {
        private const String VideoFolder = "videos";
        private const String FrameFolder = "frames";
        private const String VideoType = "video";
        private const String FrameType = "frame";
        private readonly IGenericRepository _repository;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        public DialogueVideoMerge(
            INotificationPublisher notificationPublisher,
            IServiceScopeFactory factory,
            SftpClient client,
            SftpSettings sftpSettings,
            ElasticClient log
            )
        {
            var scope = factory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            _sftpClient = client;
            _sftpSettings = sftpSettings;
            _log = log;
            _notificationPublisher = notificationPublisher;
        }

        public static FileFrame LastFrame(FileVideo video, List<FileFrame> frames)
        {
            return frames.Where(p => p.Time >= video.BegTime && p.Time <= video.EndTime).OrderByDescending(p => p.Time).FirstOrDefault();
        }

        public async Task Run(DialogueVideoMergeRun message)
        {
            _log.SetFormat("{ApplicationUserId}, {DialogueId}");
            _log.SetArgs(message.ApplicationUserId, message.DialogueId);
            try
            {

                var languageId = _repository.GetWithInclude<ApplicationUser>(p => 
                            p.Id == message.ApplicationUserId,
                            link => link.Company)
                        .First().Company.LanguageId;

                var pathClient = new PathClient();
              
                _log.Info($"Language id is {languageId}");
                var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));
            
                var ffmpeg = new FFMpegWrapper(Path.Combine(pathClient.BinPath(), "ffmpeg.exe"));

                var fileVideos = await _repository.FindByConditionAsync<FileVideo>(item => 
                        item.ApplicationUserId == message.ApplicationUserId &&
                        item.EndTime >= message.BeginTime &&
                        item.BegTime <= message.EndTime &&
                        item.FileExist);
                
                var fileFrames = await _repository.FindByConditionAsync<FileFrame>(item => 
                    item.ApplicationUserId == message.ApplicationUserId &&
                    item.Time >= message.BeginTime &&
                    item.Time <= message.EndTime &&
                    item.FileExist);

                var videos = fileVideos.OrderBy(p => p.BegTime).ToList();
                var frames = fileFrames.OrderBy(p => p.Time).ToList();

                if (!videos.Any())
                {
                    _log.Error("No video files");
                    throw new Exception("No video files");
                }

                var commands = new List<FFMpegWrapper.FFmpegCommand>();
                commands.Add(new FFMpegWrapper.FFmpegCommand{
                    Command = $"-i {Path.Combine(sessionDir, videos[0].FileName)}",
                    Path = Path.Combine(sessionDir, videos[0].FileName),
                    Type = VideoType,
                    FileFolder = VideoFolder,
                    FileName = videos[0].FileName
                });

                for (int i = 1; i < videos.Count(); i++)
                {
                    var timeGap = videos[i].BegTime.Subtract(videos[i - 1].EndTime).TotalSeconds;  
                    if (timeGap > 1)
                    {
                        var lastFrame = LastFrame(videos[i - 1], frames);
                        var frameDir = Path.Combine(sessionDir, lastFrame.FileName);
                        commands.Add(new FFMpegWrapper.FFmpegCommand
                        {
                            Command = $"-loop 1 -framerate 24 -t {timeGap} -i {frameDir}",
                            Path = frameDir,
                            Type = FrameType,
                            FileFolder = FrameFolder,
                            FileName = lastFrame.FileName
                        });
                    }

                    var videoDir = Path.Combine(sessionDir, videos[i].FileName);
                    commands.Add(new FFMpegWrapper.FFmpegCommand
                    {
                        Command = $"-i {videoDir}",
                        Path = videoDir,
                        Type = VideoType,
                        FileFolder = VideoFolder,
                        FileName = videos[i].FileName
                    });
                }


                _log.Info("Downloading all files");
                foreach (var command in commands.GroupBy(p => p.FileName).Select(p => p.First()))
                {
                    await _sftpClient.DownloadFromFtpToLocalDiskAsync($"{_sftpSettings.DestinationPath}{command.FileFolder}/{command.FileName}", sessionDir);
                }

                var extension = videos.Select(item => Path.GetExtension(item.FileName.ToString())).First();
                var basename = $"{message.DialogueId}{extension}";
                var tmpBasename = $"_tmp_{message.DialogueId}{extension}";
                var outputFn = Path.Combine(sessionDir, basename);
                var outputTmpFn = Path.Combine(sessionDir, tmpBasename);

                _log.Info("Concat videos and frames");
                var outputDialogueMerge = ffmpeg.ConcatSameCodecsAndFrames(commands, outputTmpFn, sessionDir);
                var sTime = videos.First().BegTime.ToUniversalTime();

                _log.Info("Cut result dialogue video");
                var outputCutDialogue = ffmpeg.CutBlob(outputTmpFn, 
                    outputFn,
                    (message.BeginTime - sTime).ToString(@"hh\:mm\:ss\.ff"), 
                    (message.EndTime - message.BeginTime).ToString(@"hh\:mm\:ss\.ff"));
                
                _log.Info("Uploading to FTP server result dialogue video");
                var filename = $"{message.DialogueId}{extension}";
                await _sftpClient.UploadAsync(outputTmpFn, "dialoguevideos", filename);
                
                _log.Info("Delete all local files");
                Directory.Delete(sessionDir, true);
                var @event = new VideoToSoundRun
                {
                    Path = "dialoguevideos/" + filename
                };
                _notificationPublisher.Publish(@event);
                _log.Info($"Function finished OnPremDialogueVideoMerge");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                Console.WriteLine(e);
            }
        }
    }
}