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

        public string BinPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "bin");
        }

        public string GetTempPath()
        {
            Directory.CreateDirectory("temp");
            return "temp";
        }

        public string GenSessionId()
        {
            return $"session_{Guid.NewGuid()}_{DT.Format(DateTime.Now)}";
        }

        public string GenLocalDir(string sessionId)
        {
            var path = Path.Combine(GetTempPath(), "data", sessionId + "/");
            Directory.CreateDirectory(path);
            return path;
        }

        public static FileFrame LastFrame(FileVideo video, List<FileFrame> frames)
        {
            return frames.Where(p => p.Time >= video.BegTime && p.Time <= video.EndTime).OrderByDescending(p => p.Time).FirstOrDefault();
        }

        public async Task Run(DialogueCreationRun message)
        {
            Console.WriteLine("Funciton started");
            // get language id
            var languageId = _repository.GetWithInclude<ApplicationUser>(p => 
                    p.ApplicationUserId == message.ApplicationUserId,
                    link => link.Company)
                .First().Company.LanguageId;
            
            var sessionDir = Path.GetFullPath(GenLocalDir(GenSessionId()));
            Console.WriteLine(sessionDir); 
            try
            {
            // create ffmpeg
            var ffmpeg = new FFMpegWrapper(Path.Combine(BinPath(), "ffmpeg.exe"));

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

            Console.WriteLine($"{videos.Count()}, {frames.Count()}");
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

            Console.WriteLine($"{commands}");

            // download files 
            foreach (var command in commands.GroupBy(p => p.FileName).Select(p => p.First()))
            {
                await _sftpClient.DownloadFromFtpToLocalDiskAsync($"{command.FileFolder}/{command.FileName}", sessionDir);
            }

            Console.WriteLine("Downloaded");

            var extension = videos.Select(item => Path.GetExtension(item.FileName.ToString())).First();
            var basename = $"{message.DialogueId}{extension}";
            var tmpBasename = $"_tmp_{message.DialogueId}{extension}";
            var outputFn = Path.Combine(sessionDir, basename);
            var outputTmpFn = Path.Combine(sessionDir, tmpBasename);

            var outputDialogueMerge = ffmpeg.ConcatSameCodecsAndFrames(commands, outputTmpFn, sessionDir);
            Console.WriteLine($"Result of concating videos and frames {outputDialogueMerge}");

            var sTime = videos.First().BegTime.ToUniversalTime();
            var outputCutDialogue = ffmpeg.CutBlob(outputTmpFn, 
                outputFn,
                (message.BeginTime - sTime).ToString(@"hh\:mm\:ss\.ff"), 
                (message.EndTime - message.BeginTime).ToString(@"hh\:mm\:ss\.ff"));
            
            Console.WriteLine($"Result of cuting reuslt videofile {outputCutDialogue}");

            await _sftpClient.UploadAsync(outputTmpFn, "dialogues/", $"{message.DialogueId}{extension}");
            
            // remove all local files
            Directory.Delete(sessionDir);
            Console.WriteLine("Function finished");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            

            
        }
    }
}



/*var localDir = Misc.GenLocalDir(sessionId);
                   
                    var commands = new List<FFMpegWrapper.FFmpegCommand>();
                    var firstBlobSAS = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(blobVideos[0].BlobContainer, blobVideos[0].BlobName);
                    var blobFn = Path.Combine(localDir, new Uri(firstBlobSAS).Segments[2]);

                    var blobSTime = blobVideos[0].BegTime.ToUniversalTime();
                    commands.Add(new FFMpegWrapper.FFmpegCommand
                    {
                        Command = $"-i {blobFn}",
                        BlobContainer = "videos",
                        BlobName = blobVideos[0].BlobName,
                        SAS = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(blobVideos[0].BlobContainer, blobVideos[0].BlobName),
                        Path = blobFn,
                        Type = "video"
                    });


                    for (int i = 1; i < blobVideos.Count(); i++)
                    {
                        var gap = blobVideos[i].BegTime.Subtract(blobVideos[i - 1].EndTime).TotalSeconds;
                        if (gap > 1)
                        {
                            var lastFrame = LastFrame(blobVideos[i - 1], blobFrames);
                            var lastFileName = lastFrame == null ? "black.jpg" : lastFrame.FileName;
                            var lastFileContainer = lastFrame == null ? "external" : "frames";
                            var sasFrame = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(lastFileContainer, lastFileName);
                            var blobFrameFn = Path.Combine(localDir, new Uri(sasFrame).Segments[2]);

                            commands.Add(new FFMpegWrapper.FFmpegCommand
                            {
                                BlobContainer = lastFileContainer,
                                BlobName = lastFileName,
                                Command = $"-loop 1 -framerate 24 -t {gap} -i {blobFrameFn}",
                                SAS = sasFrame,
                                Path = blobFrameFn,
                                Type = "frame"
                            });
                        }

                        var sasVideo = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(blobVideos[i].BlobContainer, blobVideos[i].BlobName);
                        var blobVideoFn = Path.Combine(localDir, new Uri(sasVideo).Segments[2]);

                        commands.Add(new FFMpegWrapper.FFmpegCommand
                        {
                            BlobContainer = "videos",
                            BlobName = blobVideos[i].BlobName,
                            Command = $"-i {blobVideoFn}",
                            SAS = sasVideo,
                            Path = blobVideoFn,
                            Type = "video"
                        });
                    }

                    foreach (var doc in commands.GroupBy(p => p.BlobName).Select(p => p.First()))
                    {
                        using (var wclient = new WebClient())
                        {
                            wclient.DownloadFile(doc.SAS, doc.Path);
                        }
                    }

                    var ext = blobVideos.Select(doc => Path.GetExtension(doc.BlobName.ToString())).ToList()[0];
                    var basename = $"{dialogueId}{ext}";
                    var tmpBasename = $"_tmp_{dialogueId}{ext}";
                    var outputFn = Path.Combine(localDir, basename);
                    var outputTmpFn = Path.Combine(localDir, tmpBasename);

                    var output = ffmpeg.ConcatSameCodecsAndFrames(commands, outputTmpFn, localDir);
                    log.Info($"Result of concating videos and frames {output}");

                    var output2 = ffmpeg.CutBlob(outputTmpFn, outputFn, (begTime - blobSTime).ToString(@"hh\:mm\:ss\.ff"), (endTime - begTime).ToString(@"hh\:mm\:ss\.ff"));
                    log.Info($"Result of cuting reuslt videofile {output2}");

                    var metadata = new Dictionary<string, string> { { "ApplicationUserId", applicationUserId},
                        { "LanguageId", langId.ToString() },
                        { "BegTime", DT.Format(begTime)},
                        { "EndTime", DT.Format(endTime)},
                        { "IsNN", Convert.ToString(isNN)}};

                    var dialogueVideosContainer = EnvVar.Get("BlobContainerDialogueVideos");
                    HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(dialogueVideosContainer, Path.GetFileName(outputFn), outputFn, metadata, topicName: $"blob-{dialogueVideosContainer}");

                    // remove all local files
                    OS.SafeDelete(localDir);
                    log.Info($"Function finished: {dir.FunctionName}");
                }




        public class BlobVideo
        {
            public ObjectId _id;
            public DateTime Time;
            public DateTime EndTime;
            public DateTime BegTime;
            public DateTime CreationTime;
            public double Duration;
            public string Status;
            public bool FileExist;
            public string BlobName;
            public string BlobContainer;
            public string ApplicationUserId;
            public int LanguageId;
        }

        public class BlobFrame
        {
            public ObjectId _id;
            public DateTime Time;
            public string ApplicationUserId;
            public bool FileExist;
            public string FaceId;
            public string FileName;
            public string Status;
            public string StatusNN;
        }


        public static BlobFrame LastFrame(BlobVideo video, List<BlobFrame> frames)
        {
            return frames.Where(p => p.Time >= video.BegTime && p.Time <= video.EndTime).OrderByDescending(p => p.Time).FirstOrDefault();
        }
    }
}
*/