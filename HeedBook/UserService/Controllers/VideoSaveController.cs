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

namespace UserService.Controllers
{
    [Route("api/[controller]")]
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
            _sftpClient = _sftpClient;
        }

        [HttpPost]
        public async Task<IActionResult> VideoToSound([FromBody] byte[] video, [FromQuery] Guid applicationUserId, [FromQuery] string begTime, [FromQuery] double? duration)
        {
            try
            {

                duration = duration == null ? 15 : duration;
                var memoryStream = new MemoryStream(video);
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
