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
using DialogueVideoAssembleService.Utils;

namespace DialogueVideoAssembleService
{
    public class DialogueVideoAssemble
    {
        private readonly string _sessionId = new PathClient().GenSessionId();
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly RecordsContext _context;
        private readonly FFMpegWrapper _wrapper;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly DialogueVideoAssembleUtils _utils;
        private readonly DialogueVideoAssembleSettings _videoSettings;


        public DialogueVideoAssemble(
            INotificationPublisher notificationPublisher,
            SftpClient client,
            SftpSettings sftpSettings,
            RecordsContext context,
            FFMpegWrapper wrapper,
            ElasticClientFactory elasticClientFactory,
            DialogueVideoAssembleUtils utils,
            DialogueVideoAssembleSettings videoSettings   
        )
        {
            _sftpClient = client;
            _sftpSettings = sftpSettings;
            _elasticClientFactory = elasticClientFactory;
            _notificationPublisher = notificationPublisher;
            _context = context;
            _wrapper = wrapper;
            _utils = utils;
            _videoSettings = videoSettings;
        }

        public async Task Run(DialogueVideoAssembleRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(message.DialogueId);

            System.Console.WriteLine("Function started");

            try
            {
                var cmd = new CMDWithOutput();

                var fileVideos = _utils.GetFileVideos(message);
                if (!fileVideos.Any())
                {
                    _log.Error("No video files");
                    return;
                }                
                
                var fileFrames = _utils.GetFileFrame(message);                
                if (!fileFrames.Any()) _log.Error("No frame files");

                var dialogue = _context.Dialogues.FirstOrDefault(p => p.DialogueId == message.DialogueId);
                if (dialogue == null) 
                {
                    _log.Error("No such dialogue in postgres db");
                    return;
                }
                var dialogueDuration = dialogue.EndTime.Subtract(dialogue.BegTime).TotalSeconds;

                var videosDuration = _utils.GetTotalVideoDuration(fileVideos, message);
                if (videosDuration / dialogueDuration < 0.6 || dialogueDuration - videosDuration > 5 * 60)
                {
                    var comment = $"Too many holes in dialogue {dialogue.DialogueId}, Dialogue duration {dialogueDuration}s, Videos duration - {videosDuration}s";
                    _log.Error(comment);
                    dialogue.StatusId = 8;
                    dialogue.Comment = comment;
                    _context.SaveChanges();
                    return;
                }
                
                var pathClient = new PathClient();
                var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));      
                System.Console.WriteLine(sessionDir);          
                
                var frameCommands = new List<FFMpegWrapper.FFmpegCommand>();
                var videoMergeCommands = new List<FFMpegWrapper.FFmpegCommand>();
                _utils.BuildFFmpegCommands(message, fileVideos, fileFrames, sessionDir, ref videoMergeCommands, ref frameCommands);    
                
                _log.Info("Downloading all files");
                await _utils.DownloadFilesLocalyAsync(videoMergeCommands, _sftpClient, _sftpSettings, _log, sessionDir);                   
                _log.Info("Running commands for frames");
                _utils.RunFrameFFmpegCommands(frameCommands, cmd, _wrapper, _log, sessionDir);

                var extension = Path.GetExtension(fileVideos.Select(item => item.FileName).FirstOrDefault());
                var tempOutputFn = Path.Combine(sessionDir, $"_tmp_{message.DialogueId}{extension}");
                var outputFn = Path.Combine(sessionDir, $"{message.DialogueId}{extension}");                                

                _log.Info("Concat videos and frames");
                // var outputDialogueMerge = _wrapper.ConcatSameCodecsAndFrames(videoMergeCommands, tempOutputFn, sessionDir);
                var outputDialogueMerge = _wrapper.ConcatSameCodecs(videoMergeCommands.Select(p => p.Path).ToList(), tempOutputFn, sessionDir);
                var outputCutVideo = _wrapper.CutBlob(
                        tempOutputFn,
                        outputFn,
                        (message.BeginTime.Subtract(fileVideos.Min(p => p.BegTime)).ToString(@"hh\:mm\:ss\.ff")),
                        (message.EndTime.Subtract(message.BeginTime).ToString(@"hh\:mm\:ss\.ff")));
                _log.Info("Uploading to FTP server result dialogue video");
                await _sftpClient.UploadAsync(outputFn, "dialoguevideos", $"{message.DialogueId}{extension}");

                _log.Info("Send message to video to sound");
                _utils.SendMessageToVideoToSound(message, extension, _notificationPublisher);
                
                _log.Info("Delete all local files");
                Directory.Delete(sessionDir, true);
                _log.Info("Function finished OnPremDialogueAssembleMerge");
            }
            catch (SftpPathNotFoundException e)
            {
                _log.Error($"Exception occured with this input parameters\n"
                + $"ApplicationUserId: {message.ApplicationUserId}, \n{message.DialogueId}, \n{message.BeginTime}, \n{message.EndTime}");
                _log.Fatal($"{e}");
            }
            catch (Exception e)
            {
                _log.Fatal($" Exception occured with this input parameters\n"
                + $"ApplicationUserId: {message.ApplicationUserId}, \nDialogueId: {message.DialogueId}, \nBeginTime: {message.BeginTime}, \nEndTime: {message.EndTime}");
                _log.Fatal($" DialogueId: {message.DialogueId} \nException occured {e}");
                throw;
            }
        }
    }
}