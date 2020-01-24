using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HBData;
using HBData.Models;
using System.Reflection;
using RabbitMqEventBus.Events;
using HBLib.Utils;
using HBLib;
using RabbitMqEventBus;
using System.IO;

namespace DialogueVideoAssembleService.Utils
{
    public class DialogueVideoAssembleUtils
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly DialogueVideoAssembleSettings _videoSettings;
        private readonly FFMpegWrapper _wrapper;


        public DialogueVideoAssembleUtils(RecordsContext context,
             IConfiguration config,
             FFMpegWrapper wrapper,
             DialogueVideoAssembleSettings videoSettings)
        {
            _context = context;
            _config = config;
            _videoSettings = videoSettings;
            _wrapper = wrapper;
        }

        public List<FileVideo> GetFileVideos(DialogueVideoAssembleRun message)
        {
            return _context.FileVideos
                    .Where(p => p.ApplicationUserId == message.ApplicationUserId
                        && p.EndTime >= message.BeginTime
                        && p.BegTime <= message.EndTime
                        && p.FileExist)
                    .OrderBy(p => p.BegTime)
                    .ToList(); 
        }

        public List<FileFrame> GetFileFrame(DialogueVideoAssembleRun message)
        {
            return _context.FileFrames
                    .Where(p => p.ApplicationUserId == message.ApplicationUserId
                        && p.Time >= message.BeginTime
                        && p.Time <= message.EndTime
                        && p.FileExist)
                    .OrderBy(p => p.Time)
                    .ToList();
        }

        public double? GetTotalVideoDuration(List<FileVideo> fileVideos, DialogueVideoAssembleRun message)
        {
            return fileVideos.Sum(p => MinTime(message.EndTime, p.EndTime).Subtract(MaxTime(message.BeginTime, p.BegTime)).TotalSeconds);
        }

        private DateTime MaxTime(DateTime dt1, DateTime dt2)
        {
            if (dt1 > dt2) return dt1;
            return dt2; 
        }

        private DateTime MinTime(DateTime dt1, DateTime dt2)
        {
            if (dt1 < dt2) return dt1;
            return dt2; 
        }

        public async Task DownloadFilesLocalyAsync(List<FFMpegWrapper.FFmpegCommand> videoMergeCommands, SftpClient sftpClient, 
            SftpSettings sftpSettings, ElasticClient log, string sessionDir, bool isExtended)
        {
            if (isExtended)
            {
                foreach (var command in videoMergeCommands.GroupBy(p => p.FileName).Select(p => p.First()))
                {
                    log.Info($"Downloading file {command.FileName}");                        
                    await sftpClient.DownloadFromFtpToLocalDiskAsync(
                        $"{sftpSettings.DestinationPath}{command.FileFolder}/{command.FileName}", sessionDir);                    
                } 
            }
            else
            {
                foreach (var command in videoMergeCommands.GroupBy(p => p.FileName).Select(p => p.First())
                    .Where(p => p.Type ==  _videoSettings.VideoType))
                {
                    System.Console.WriteLine($"Downloading video -- {command.FileName}");
                    await sftpClient.DownloadFromFtpToLocalDiskAsync(
                        $"{sftpSettings.DestinationPath}{command.FileFolder}/{command.FileName}", sessionDir);
                }

                foreach (var command in videoMergeCommands.GroupBy(p => p.FileName).Select(p => p.First())
                    .Where(p => p.Type ==  _videoSettings.FrameType))
                {
                    System.Console.WriteLine($"Creating frame -- {command.FileName}");
                    var res = await _wrapper.GetLastFrameFromVideo(command.InsideVideoPath, Path.Combine(sessionDir, command.FileName));
                    System.Console.WriteLine(res);
                }
            }
        }

        public void RunFrameFFmpegCommands(List<FFMpegWrapper.FFmpegCommand> frameCommands, CMDWithOutput cmd, FFMpegWrapper wrapper, ElasticClient log, string sessionDir)
        {
            foreach (var frameCommand in frameCommands)
            {
                var output = cmd.runCMD(wrapper.FfPath,
                    $"-loop 1 {frameCommand.Command} -pix_fmt yuv420p {frameCommand.ImagePath}");
                log.Info($"Result of ffmpeg command {frameCommand.Command} is {output}");

                var pathToMerge = new List<string>();
                for (var i = 0; i < frameCommand.Duration; i++)
                {   
                    pathToMerge.Add(frameCommand.ImagePath);
                }
                var resConcating = wrapper.ConcatSameCodecs(pathToMerge, frameCommand.Path, sessionDir);
            }
        }

        public void SendMessageToVideoToSound(DialogueVideoAssembleRun message, string extension, INotificationPublisher notificationPublisher)
        {
            var @event = new VideoToSoundRun
            {
                Path = $"dialoguevideos/{message.DialogueId}{extension}"
            };
            notificationPublisher.Publish(@event);
        }

        public void BuildFFmpegCommands(
            DialogueVideoAssembleRun message,
            List<FileVideo> fileVideos,
            List<FileFrame> fileFrames,
            string sessionDir,
            ref List<FFMpegWrapper.FFmpegCommand> videoMergeCommands,
            ref List<FFMpegWrapper.FFmpegCommand> frameCommands)
        {
            for (var i = 0; i < fileVideos.Count(); i++)
            {
                if (i > 0)
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
                    Type = _videoSettings.VideoType,
                    FileFolder = _videoSettings.VideoFolder,
                    FileName = fileVideos[i].FileName
                });
            }
            var lastVideoTimeGap = Convert.ToInt32(message.EndTime.Subtract(fileVideos.Last().EndTime).TotalSeconds);            
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
                var tempImageShortVideoPath = Path.Combine(sessionDir, $"_short_{tempImageVideoName}");
                videoMergeCommands.Add(new FFMpegWrapper.FFmpegCommand
                {
                    Command = $"-i {tempImageVideoPath}",                    
                    Path = tempImageVideoPath,
                    Type = _videoSettings.FrameType,
                    FileFolder = _videoSettings.FrameFolder,
                    FileName = lastFrame.FileName,
                    InsideVideoPath = Path.Combine(sessionDir, fileVideos[index - 1].FileName)
                });

                frameCommands.Add(new FFMpegWrapper.FFmpegCommand
                {
                    Command = $"-i {frameDir} -c:v libx264 -t 1",
                    Path = tempImageVideoPath,
                    ImagePath = tempImageShortVideoPath,
                    FileName = tempImageVideoName,
                    Duration = timeGap
                });
            }   
        }

        public static FileFrame LastFrame(FileVideo video, List<FileFrame> frames)
        {
            return frames.Where(p => p.Time >= video.BegTime && p.Time <= video.EndTime).OrderByDescending(p => p.Time)
                         .FirstOrDefault();
        }
    }
}