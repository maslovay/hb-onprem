using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBLib.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class VideoSaveController : Controller
    {
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;

        public VideoSaveController(INotificationHandler handler, RecordsContext context, SftpClient sftpClient)
        {
            _handler = handler;
            _context = context;
            _sftpClient = sftpClient;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
        public async Task<IActionResult> VideoSave([FromQuery] Guid applicationUserId,
            [FromQuery] String begTime,
            [FromQuery] Double? duration,
            [FromForm] IFormCollection formData)
        {
            try
            {   
                System.Console.WriteLine("1");
                duration = duration == null ? 15 : duration;
                System.Console.WriteLine("2");
                var memoryStream = formData.Files.FirstOrDefault().OpenReadStream();
                System.Console.WriteLine("3");
                if (memoryStream == null)   return BadRequest("No video file or file is empty");
                System.Console.WriteLine("4");
                var languageId = _context.ApplicationUsers
                                         .Include(p => p.Company)
                                         .Include(p => p.Company.Language)
                                         .Where(p => p.Id == applicationUserId)
                                         .First().Company.Language.LanguageId;

                var stringFormat = "yyyyMMddhhmmss";
                var time = DateTime.ParseExact(begTime, stringFormat, CultureInfo.InvariantCulture);
                var fileName = $"{applicationUserId}_{time.ToString(stringFormat)}_{languageId}.mkv";
                System.Console.WriteLine("5");
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, "videos/", fileName);
                System.Console.WriteLine("6");

                var videoFile = new FileVideo();
                videoFile.ApplicationUserId = applicationUserId;
                videoFile.BegTime = time;
                videoFile.CreationTime = DateTime.UtcNow;
                videoFile.Duration = duration;
                videoFile.EndTime = time.AddSeconds((Double) duration);
                videoFile.FileContainer = "videos";
                videoFile.FileExist = await _sftpClient.IsFileExistsAsync($"{fileName}");
                videoFile.FileName = fileName;
                videoFile.FileVideoId = Guid.NewGuid();
                videoFile.StatusId = 6;

                _context.FileVideos.Add(videoFile);
                System.Console.WriteLine("7");
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
                    Console.WriteLine($"No such file videos/{fileName}");
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