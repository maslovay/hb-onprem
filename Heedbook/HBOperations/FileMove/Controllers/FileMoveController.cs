using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBLib.Utils;
using HBMLHttpClient.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FileMove.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileMoveController : ControllerBase
    {
        private readonly RecordsContext _context;
        private readonly SftpClient _client;
        private readonly BlobController _controller;
        public FileMoveController(RecordsContext context,
            SftpClient client, 
            BlobController controller)
        {
            _context = context;
            _client = client;
            _controller = controller;
        }

        [HttpPost("isAvatarExists")]
        public async Task<IActionResult> GetBlob()
        {
            var clientProfiles = _context.DialogueClientProfiles
                .Where(item => item.Dialogue.CreationTime >= DateTime.UtcNow.AddDays(-30) && item.Dialogue.StatusId == 3)
                .ToList();
            var countExists = 0;
            var count = clientProfiles.Count;
            foreach (var dialogueClientProfile in clientProfiles)
            {
                if ( await _controller.IsFileExists(dialogueClientProfile.Avatar, "clientavatars"))
                {
                    countExists += 1;
                }
            }

            return Ok(new
            {
                count,
                countExists
            });
        }
        // GET api/values
        [HttpPost]
        public async Task<IActionResult> Get()
        {
            var clientProfiles = _context.DialogueClientProfiles
                .Include(item => item.Dialogue)
                .Where(item => item.Dialogue.CreationTime >= DateTime.UtcNow.AddDays(-30) && item.Dialogue.StatusId == 3)
                .ToList();
            foreach (var clientProfile in clientProfiles)
            {
                var path = string.Concat("clientavatars/", clientProfile.DialogueId, ".jpg");
                if (!await _client.IsFileExistsAsync(path))
                {
                    await CreateAvatar(new Message
                    {
                        BeginTime = clientProfile.Dialogue.BegTime,
                        EndTime = clientProfile.Dialogue.EndTime,
                        ApplicationUserId = clientProfile.Dialogue.ApplicationUserId,
                        DialogueId = clientProfile.DialogueId.Value
                    });
                }
            }
            return Ok();
        }

        private async Task CreateAvatar(Message message)
        {
            var frames =
                _context.FileFrames
                    .Include(p => p.FrameAttribute)
                    .Include(p => p.FrameEmotion)
                    .Where(item =>
                        item.ApplicationUserId == message.ApplicationUserId
                        && item.Time >= message.BeginTime
                        && item.Time <= message.EndTime)
                    .ToList();


            var attributes = frames.Where(p => p.FrameAttribute.Any())
                .Select(p => p.FrameAttribute.First())
                .ToList();

            if (attributes.Any())
            {
                var attribute = attributes.First();

                var localPath =
                    await _client.DownloadFromFtpToLocalDiskAsync("frames/" + attribute.FileFrame.FileName);

                var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(attribute.Value);

                var rectangle = new Rectangle
                {
                    Height = faceRectangle.Height,
                    Width = faceRectangle.Width,
                    X = faceRectangle.Top,
                    Y = faceRectangle.Left
                };

                using (var stream = FaceDetection.CreateAvatar(localPath, rectangle))
                {
                    await _client.UploadAsMemoryStreamAsync(stream, "clientavatars/", $"{message.DialogueId}.jpg");
                }

                Console.WriteLine($"Uploaded avatar: " + message.DialogueId + ".jpg");
            }
        }
    }
}