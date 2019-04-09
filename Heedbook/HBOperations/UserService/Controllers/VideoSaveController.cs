using System;
using System.IO;
using System.Linq;
using HBData;
using HBData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notifications.Base;
using RabbitMqEventBus.Events;
using HBLib.Utils;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class VideoSaveController : Controller
    {
        private readonly INotificationHandler _handler;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;

        public VideoSaveController(INotificationHandler handler, RecordsContext context, SftpClient sftpClient)
        {
            _handler = handler;
            _context = context;
            _sftpClient = sftpClient;
        }

        [HttpPost("VideoSave")]
        public async Task<IActionResult> VideoSave([FromQuery] Guid applicationUserId, 
            [FromQuery] string begTime,
            [FromQuery] double? duration,
            [FromBody] string video)
        {
            try
            {
                duration = duration == null ? 15 : duration;
                var imgBytes = Convert.FromBase64String(video);
                var memoryStream = new MemoryStream(imgBytes);
                var languageId = _context.ApplicationUsers
                    .Include(p => p.Company)
                    .Include(p => p.Company.Language)
                    .Where(p => p.Id == applicationUserId)
                    .First().Company.Language.LanguageId;

                var stringFormat = "yyyyMMddhhmmss";
                var time =  DateTime.ParseExact(begTime, stringFormat, CultureInfo.InvariantCulture);
                var fileName = $"{applicationUserId}_{time.ToString(stringFormat)}_{languageId}.mkv";
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, "videos/", fileName);
                
                var videoFile = new FileVideo();
                videoFile.ApplicationUserId = applicationUserId;
                videoFile.BegTime = time;
                videoFile.CreationTime = DateTime.UtcNow;
                videoFile.Duration = duration;
                videoFile.EndTime = time.AddSeconds((double) duration);
                videoFile.FileContainer = "videos";
                videoFile.FileExist =  await _sftpClient.IsFileExistsAsync($"videos/{fileName}");
                videoFile.FileName = fileName;
                videoFile.FileVideoId = Guid.NewGuid();
                videoFile.StatusId = 6;

                _context.FileVideos.Add(videoFile);
                _context.SaveChanges();


                if (videoFile.FileExist)
                {
                    var message = new FramesFromVideoRun();
                    message.Path = $"videos/{fileName}";
                    Console.WriteLine($"Sending message {JsonConvert.SerializeObject(message)}");
                    _handler.EventRaised(message);
                }
                else
                {
                    Console.WriteLine($"No such file {fileName}");
                }
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
            

            // _handler.EventRaised(message);
        }
    }
}
