using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;

namespace DialogueVideoAssembleService
{
    public class DialogueVideoAssemble
    {
        private const String VideoFolder = "videos";
        private const String FrameFolder = "frames";
        private const String VideoType = "video";
        private const String FrameType = "frame";
        private readonly string _videosFolder = "videos";
        private readonly string _sessionId = new PathClient().GenSessionId();

        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly RecordsContext _context;
        private readonly FFMpegWrapper _wrapper;
        private DialogueVideoAssembleRun _message;
        private readonly ElasticClientFactory _elasticClientFactory;


        public DialogueVideoAssemble(
            INotificationPublisher notificationPublisher,
            SftpClient client,
            SftpSettings sftpSettings,
            ElasticClient log,
            RecordsContext context,
            FFMpegWrapper wrapper,
            ElasticClientFactory elasticClientFactory
        )
        {
            _sftpClient = client;
            _sftpSettings = sftpSettings;
            _elasticClientFactory = elasticClientFactory;
            _notificationPublisher = notificationPublisher;
            _context = context;
            _wrapper = wrapper;
        }

        public async Task Run(DialogueVideoAssembleRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _message = message;
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(message.DialogueId);
            try
            {
                var cmd = new CMDWithOutput();   
                
                var fileVideos = _context.FileVideos.Where(p => p.ApplicationUserId == message.ApplicationUserId
                                                                && p.EndTime >= message.BeginTime
                                                                && p.BegTime <= message.EndTime
                                                                && p.FileExist)
                                         .OrderBy(p => p.BegTime)
                                         .ToList();                            
                if (!fileVideos.Any())
                {
                    _log.Error("No video files");
                    return;
                }                
                
                var fileFrames = _context.FileFrames.Where(p => p.ApplicationUserId == message.ApplicationUserId
                                                                && p.Time >= message.BeginTime
                                                                && p.Time <= message.EndTime
                                                                && p.FileExist)
                                         .OrderBy(p => p.Time)
                                         .ToList();                
                if (!fileFrames.Any())
                {
                    _log.Error("No frame files");
                }

                var actualLength = new TimeSpan();
                foreach (var item in fileVideos)
                {
                    if (item.BegTime < message.BeginTime)
                        actualLength = actualLength.Add(item.EndTime.Subtract(message.BeginTime));
                    else if (item.EndTime > message.EndTime)
                        actualLength = actualLength.Add(message.EndTime.Subtract(item.BegTime));
                    else
                        actualLength = actualLength.Add(item.EndTime.Subtract(item.BegTime));
                }

                var badDialogue = (actualLength.TotalSeconds / (message.EndTime - message.BeginTime).TotalSeconds) <
                                  0.8;
                
                var pathClient = new PathClient();

                var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));                

                var frameCommands = new List<FFMpegWrapper.FFmpegCommand>();
                
                var videoMergeCommands = new List<FFMpegWrapper.FFmpegCommand>();
                
                BuildFFmpegCommands(fileVideos, fileFrames, sessionDir, ref videoMergeCommands, ref frameCommands);    
                
                _log.Info("Downloading all files");

                foreach (var command in videoMergeCommands.GroupBy(p => p.FileName).Select(p => p.First()))
                {                        
                    await _sftpClient.DownloadFromFtpToLocalDiskAsync(
                        $"{_sftpSettings.DestinationPath}{command.FileFolder}/{command.FileName}", sessionDir);                    
                }                    

                foreach (var frameCommand in frameCommands)
                {
                    var output = cmd.runCMD(_wrapper.FfPath,
                        $"-loop 1 {frameCommand.Command} -pix_fmt yuv420p {frameCommand.Path}");
                }

                var extension = Path.GetExtension(fileVideos.Select(item => item.FileName).First());
                var tempOutputFn = Path.Combine(sessionDir, $"_tmp_{message.DialogueId}{extension}");
                var outputFn = Path.Combine(sessionDir, $"{message.DialogueId}{extension}");                                

                _log.Info("Concat videos and frames");
                var outputDialogueMerge = _wrapper.ConcatSameCodecsAndFrames(videoMergeCommands, tempOutputFn, sessionDir);
                
                var outputCutVideo = _wrapper.CutBlob(
                        tempOutputFn,
                        outputFn,
                        (message.BeginTime.Subtract(fileVideos.FirstOrDefault().BegTime).ToString(@"hh\:mm\:ss\.ff")),
                        (message.EndTime.Subtract(message.BeginTime).ToString(@"hh\:mm\:ss\.ff")));

                _log.Info("Uploading to FTP server result dialogue video");
                await _sftpClient.UploadAsync(outputFn, "dialoguevideos", $"{message.DialogueId}{extension}");

                if (badDialogue)
                {
                    _log.Info("Bad dialogue");
                    _context.Dialogues.First(p => p.DialogueId == message.DialogueId).StatusId = 8;
                    _context.SaveChanges();
                }
                else
                {
                    _log.Info("Send message to video to sound");
                    var @event = new VideoToSoundRun
                    {
                        Path = $"dialoguevideos/{message.DialogueId}{extension}"
                    };
                    _notificationPublisher.Publish(@event);
                }
                
                _log.Info("Delete all local files");
                Directory.Delete(sessionDir, true);
                _log.Info("Function finished OnPremDialogueAssembleMerge");
            }
            catch (SftpPathNotFoundException e)
            {
                _log.Fatal($"Exception occured with this input parameters\n"
                + $"ApplicationUserId: {message.ApplicationUserId}, \n{message.DialogueId}, \n{message.BeginTime}, \n{message.EndTime}");
                _log.Fatal($"{e}");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured with this input parameters\n"
                + $"ApplicationUserId: {message.ApplicationUserId}, \n{message.DialogueId}, \n{message.BeginTime}, \n{message.EndTime}");
                _log.Fatal($"Exception occured {e}");
                throw;
            }
        }

        private void BuildFFmpegCommands(List<FileVideo> fileVideos,
            List<FileFrame> fileFrames,
            string sessionDir,
            ref List<FFMpegWrapper.FFmpegCommand> videoMergeCommands,
            ref List<FFMpegWrapper.FFmpegCommand> frameCommands)
        {
            
            for (var i = 0; i < fileVideos.Count(); i++)
            {
                if(i>0)
                {
                    var timeGap = Convert.ToInt32(fileVideos[i].BegTime.Subtract(fileVideos[i - 1].EndTime).TotalSeconds);
                    if (timeGap > 1)
                    {
                        AddFrameCommands(fileVideos, fileFrames, sessionDir, ref videoMergeCommands, ref frameCommands, i, timeGap);           
                    }
                }                
                
                var videoDir = Path.Combine(sessionDir, fileVideos[i].FileName);
                videoMergeCommands.Add(new FFMpegWrapper.FFmpegCommand
                {
                    Command = $"-i {videoDir}",
                    Path = videoDir,
                    Type = VideoType,
                    FileFolder = _videosFolder,
                    FileName = fileVideos[i].FileName
                });
            }
            var lastVideoTimeGap = Convert.ToInt32(_message.EndTime.Subtract(fileVideos.Last().EndTime).TotalSeconds);            
            if(lastVideoTimeGap > 1)
            {
                AddFrameCommands(fileVideos, 
                                fileFrames, 
                                sessionDir, 
                                ref videoMergeCommands, 
                                ref frameCommands, 
                                fileVideos.Count, 
                                lastVideoTimeGap+1);                                       
            }

        }
        private void AddFrameCommands(
            List<FileVideo> fileVideos,
            List<FileFrame> fileFrames,
            string sessionDir,
            ref List<FFMpegWrapper.FFmpegCommand> videoMergeCommands,
            ref List<FFMpegWrapper.FFmpegCommand> frameCommands,
            int index,
            int timeGap)        
        {
            var lastFrame = LastFrame(fileVideos[index - 1], fileFrames);
            if(lastFrame != null)
            {
                var frameDir = Path.Combine(sessionDir, lastFrame.FileName);
                var baseName = Path.GetFileNameWithoutExtension(lastFrame.FileName);
                var tempImageVideoName = $"_tmp_{baseName}.mkv";
                var tempImageVideoPath = Path.Combine(sessionDir, tempImageVideoName);
                videoMergeCommands.Add(new FFMpegWrapper.FFmpegCommand
                {
                    Command = $"-i {tempImageVideoPath}",                    
                    Path = tempImageVideoPath,
                    Type = VideoType,
                    FileFolder = FrameFolder,
                    FileName = lastFrame.FileName
                });

                frameCommands.Add(new FFMpegWrapper.FFmpegCommand
                {
                    Command = $"-i {frameDir} -c:v libx264 -t {timeGap}",
                    Path = tempImageVideoPath,
                    FileName = tempImageVideoName
                });
            }   
        }

        private static FileFrame LastFrame(FileVideo video, List<FileFrame> frames)
        {
            return frames.Where(p => p.Time >= video.BegTime && p.Time <= video.EndTime).OrderByDescending(p => p.Time)
                         .FirstOrDefault();
        }
    }
}