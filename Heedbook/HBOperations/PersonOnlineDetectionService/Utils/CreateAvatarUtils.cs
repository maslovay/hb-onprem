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
            System.Console.WriteLine($"Attribute is {attribute}");
            var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
            System.Console.WriteLine("Downloaded");
            var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(attribute);
            System.Console.WriteLine($"Rectangle {JsonConvert.SerializeObject(faceRectangle)}");
            var rectangle = new Rectangle
            {
                Height = faceRectangle.Height,
                Width = faceRectangle.Width,
                X = faceRectangle.Top,
                Y = faceRectangle.Left
            };

            var stream = FaceDetection.CreateAvatar(localPath, rectangle);
            stream.Seek(0, SeekOrigin.Begin);
            System.Console.WriteLine("Upload");
            await _sftpClient.UploadAsMemoryStreamAsync(stream, "useravatars/", $"{clientId}.jpg");
            stream.Close();

            System.Console.WriteLine(path);
            await _sftpClient.DeleteFileIfExistsAsync(path);
        }

        public async System.Threading.Tasks.Task DeleteFileAsync(string path)
        {
            await _sftpClient.DeleteFileIfExistsAsync(path);
        } 

    }
}