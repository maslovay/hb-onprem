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

        public DialogueVideoAssemble(
            INotificationPublisher notificationPublisher,
            SftpClient client,
            SftpSettings sftpSettings,
            ElasticClient log,
            RecordsContext context,
            FFMpegWrapper wrapper
        )
        {
            _sftpClient = client;
            _sftpSettings = sftpSettings;
            _log = log;
            _notificationPublisher = notificationPublisher;
            _context = context;
            _wrapper = wrapper;
        }

        public async Task Run(DialogueVideoAssembleRun message)
        {
            _log.SetFormat("{ApplicationUserId}, {DialogueId}");
            _log.SetArgs(message.ApplicationUserId, message.DialogueId);
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
                var tempFirstVideoName = $"_tmp_{fileVideos[0].FileName}";
                var tempFirstVideoPath = Path.Combine(sessionDir, tempFirstVideoName);

                var tempLastVideoName = $"_tmp_{fileVideos[fileVideos.Count - 1].FileName}";
                var tempLastVideoPath = Path.Combine(sessionDir, tempLastVideoName);

                var frameCommands = new List<FFMpegWrapper.FFmpegCommand>();
                
                var videoMergeCommands = new List<FFMpegWrapper.FFmpegCommand>
                {
                    new FFMpegWrapper.FFmpegCommand
                    {
                        Command = $"-i {tempFirstVideoPath}",
                        Path = tempFirstVideoPath,
                        Type = VideoType,
                        FileFolder = _videosFolder,
                        FileName = fileVideos[0].FileName
                    }
                };
                
                BuildFFmpegCommands(fileVideos, fileFrames, sessionDir, ref videoMergeCommands, ref frameCommands);

                if(fileVideos.Count > 1)
                {
                    videoMergeCommands.Add(new FFMpegWrapper.FFmpegCommand
                    {
                        Command = $"-i {tempLastVideoPath}",
                        Path = tempLastVideoPath,
                        Type = VideoType,
                        FileFolder = _videosFolder,
                        FileName = fileVideos[fileVideos.Count - 1].FileName
                    });
                }
                
                _log.Info("Downloading all files");

                foreach (var command in videoMergeCommands.GroupBy(p => p.FileName).Select(p => p.First()))
                {                        
                    await _sftpClient.DownloadFromFtpToLocalDiskAsync(
                        $"{_sftpSettings.DestinationPath}{command.FileFolder}/{command.FileName}", sessionDir);                    
                }
                    

                var outputCutFirstVideo = _wrapper.CutBlob(
                    Path.Combine(sessionDir, fileVideos[0].FileName),
                    tempFirstVideoPath,
                    message.BeginTime.Subtract(fileVideos[0].BegTime).ToString(@"hh\:mm\:ss\.ff"),
                    (fileVideos[0].EndTime.Subtract(fileVideos[0].BegTime)).ToString(@"hh\:mm\:ss\.ff"));

                if(fileVideos.Count > 1)
                {
                    var outputCutLastVideo = _wrapper.CutBlob(
                        Path.Combine(sessionDir, fileVideos[fileVideos.Count - 1].FileName),
                        tempLastVideoPath,
                        "00:00:00.00",
                        (message.EndTime - fileVideos[fileVideos.Count - 1].BegTime).ToString(@"hh\:mm\:ss\.ff"));
                }                

                foreach (var frameCommand in frameCommands)
                {
                    var output = cmd.runCMD(_wrapper.FfPath,
                        $"-loop 1 {frameCommand.Command} -pix_fmt yuv420p {frameCommand.Path}");
                }

                var extension = Path.GetExtension(fileVideos.Select(item => item.FileName).First());
                var outputFn = Path.Combine(sessionDir, $"{message.DialogueId}{extension}");
                
                _log.Info("Concat videos and frames");
                var outputDialogueMerge = _wrapper.ConcatSameCodecsAndFrames(videoMergeCommands, outputFn, sessionDir);
                
                _log.Info("Uploading to FTP server result dialogue video");
                await _sftpClient.UploadAsync(outputFn, "dialoguevideos", $"{message.DialogueId}{extension}");

                if (badDialogue)
                {
                    _context.Dialogues.First(p => p.DialogueId == message.DialogueId).StatusId = 8;
                    _context.SaveChanges();
                }
                else
                {
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

            for (var i = 1; i < fileVideos.Count(); i++)
            {
                var timeGap =
                    Convert.ToInt32(fileVideos[i].BegTime.Subtract(fileVideos[i - 1].EndTime).TotalSeconds);
                if (timeGap > 1)
                {
                    var lastFrame = LastFrame(fileVideos[i - 1], fileFrames);
                    if(lastFrame == null) continue;
                    var frameDir = Path.Combine(sessionDir, lastFrame.FileName);
                    var baseName = Path.GetFileNameWithoutExtension(lastFrame.FileName);
                    var tempImageVideoName = $"_tmp_{baseName}.mkv";
                    var tempImageVideoPath = Path.Combine(sessionDir, tempImageVideoName);
                    videoMergeCommands.Add(new FFMpegWrapper.FFmpegCommand
                    {
                        Command = $"-i {tempImageVideoPath}",
                        //Path = Path.GetFullPath(tempImageVideoPath),
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

                if (i >= fileVideos.Count() - 1) continue;
                var videoDir = Path.Combine(_sessionId, fileVideos[i].FileName);
                videoMergeCommands.Add(new FFMpegWrapper.FFmpegCommand
                {
                    Command = $"-i {videoDir}",
                    Path = videoDir,
                    Type = VideoType,
                    FileFolder = _videosFolder,
                    FileName = fileVideos[i].FileName
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