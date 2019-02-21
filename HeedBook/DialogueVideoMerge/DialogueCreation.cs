﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqEventBus.Events;

namespace DialogueVideoMerge
{
    public class DialogueCreation
    {
        private const String VideoFolder = "videos";
        private const String FrameFolder = "frames";
        private const String VideoType = "video";
        private const String FrameType = "frame";
        private readonly IGenericRepository _repository;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        public DialogueCreation(IServiceScopeFactory factory,
            SftpClient client,
            SftpSettings sftpSettings)
        {
            var scope = factory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            _sftpClient = client;
            _sftpSettings = sftpSettings;
        }

        public static FileFrame LastFrame(FileVideo video, List<FileFrame> frames)
        {
            return frames.Where(p => p.Time >= video.BegTime && p.Time <= video.EndTime).OrderByDescending(p => p.Time).FirstOrDefault();
        }

        public async Task Run(DialogueCreationRun message)
        {
            var pathClient = new PathClient();

            // get language id
            var languageId = _repository.GetWithInclude<ApplicationUser>(p => 
                    p.ApplicationUserId == message.ApplicationUserId,
                    link => link.Company)
                .First().Company.LanguageId;
            
            var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));
            try
            {
                // create ffmpeg
                var ffmpeg = new FFMpegWrapper(Path.Combine(pathClient.BinPath(), "ffmpeg.exe"));

                // get info about vide files
                var fileVideos = await _repository.FindByConditionAsync<FileVideo>(item => 
                        item.ApplicationUserId == message.ApplicationUserId &&
                        item.EndTime >= message.BeginTime &&
                        item.BegTime <= message.EndTime &&
                        item.FileExist);
                
                // get info about frames files
                var fileFrames = await _repository.FindByConditionAsync<FileFrame>(item => 
                    item.ApplicationUserId == message.ApplicationUserId &&
                    item.Time >= message.BeginTime &&
                    item.Time <= message.EndTime &&
                    item.FileExist);

                // to list
                var videos = fileVideos.OrderBy(p => p.BegTime).ToList();
                var frames = fileFrames.OrderBy(p => p.Time).ToList();

                if (videos.Count() == 0)
                {
                    throw new Exception("No video files");
                }
                // commands list
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


                // download files 
                foreach (var command in commands.GroupBy(p => p.FileName).Select(p => p.First()))
                {
                    await _sftpClient.DownloadFromFtpToLocalDiskAsync($"{_sftpSettings.DestinationPath}{command.FileFolder}/{command.FileName}", sessionDir);
                }

                var extension = videos.Select(item => Path.GetExtension(item.FileName.ToString())).First();
                var basename = $"{message.DialogueId}{extension}";
                var tmpBasename = $"_tmp_{message.DialogueId}{extension}";
                var outputFn = Path.Combine(sessionDir, basename);
                var outputTmpFn = Path.Combine(sessionDir, tmpBasename);

                var outputDialogueMerge = ffmpeg.ConcatSameCodecsAndFrames(commands, outputTmpFn, sessionDir);

                var sTime = videos.First().BegTime.ToUniversalTime();
                var outputCutDialogue = ffmpeg.CutBlob(outputTmpFn, 
                    outputFn,
                    (message.BeginTime - sTime).ToString(@"hh\:mm\:ss\.ff"), 
                    (message.EndTime - message.BeginTime).ToString(@"hh\:mm\:ss\.ff"));
                
                await _sftpClient.UploadAsync(outputTmpFn, "dialoguevideos", $"{message.DialogueId}{extension}");
                
                // remove all local files
                Directory.Delete(sessionDir, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}