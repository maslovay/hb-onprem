using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;

namespace VideoToSoundService
{
    public class VideoToSound
    {
        private readonly IConfiguration _configuration;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        public VideoToSound(IConfiguration configuration, 
            SftpClient sftpClient,
            SftpSettings sftpSettings)
        {
            _configuration = configuration;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
        }
        
        public async Task Run(String path)
        {
            var dialogueId = Path.GetFileNameWithoutExtension(path.Split('/').Last());
            var localVideoPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
            var localAudioPath = Path.Combine(_sftpSettings.DownloadPath + dialogueId + ".wav");
            var ffmpeg = new FFMpegWrapper(_configuration["FfmpegPath"]);
            ffmpeg.VideoToWav(localVideoPath, localAudioPath);
            if (!File.Exists(localAudioPath))
            {
                OS.SafeDelete(localVideoPath);
            }
            
        }
    }
}