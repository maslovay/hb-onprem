using HBLib.Utils;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;
using PersonOnlineDetectionService.Models;
using System;

namespace PersonOnlineDetectionService.Utils
{
    public class CreateAvatarUtils
    {
        private readonly SftpClient _sftpClient;
        public CreateAvatarUtils(SftpClient sftpClient)
        {
            _sftpClient = sftpClient;
        }

        public async System.Threading.Tasks.Task ExecuteAsync(string attribute, Guid clientId, string path)
        {
            var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
            
            var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(attribute);
            var rectangle = new Rectangle
            {
                Height = faceRectangle.Height,
                Width = faceRectangle.Width,
                X = faceRectangle.Top,
                Y = faceRectangle.Left
            };

            var stream = FaceDetection.CreateAvatar(localPath, rectangle);
            stream.Seek(0, SeekOrigin.Begin);
            await _sftpClient.UploadAsMemoryStreamAsync(stream, "clientavatars/", $"{clientId}.jpg");
            stream.Close();
            await _sftpClient.DeleteFileIfExistsAsync(path);
        }

        public async System.Threading.Tasks.Task DeleteFileAsync(string path)
        {
            await _sftpClient.DeleteFileIfExistsAsync(path);
        } 

    }
}