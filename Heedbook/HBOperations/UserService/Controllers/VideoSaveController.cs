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
//        private readonly ElasticClient _log;


        public VideoSaveController(INotificationHandler handler, RecordsContext context, SftpClient sftpClient/*, ElasticClient log*/)
        {
            _handler = handler;
            _context = context;
            _sftpClient = sftpClient;
//            _log = log;
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
//                _log.Info("Function Video save info started");
                duration = duration == null ? 15 : duration;
                var memoryStream = formData.Files.FirstOrDefault().OpenReadStream();
                if (memoryStream == null)   return BadRequest("No video file or file is empty");
                var languageId = _context.ApplicationUsers
                                         .Include(p => p.Company)
                                         .Include(p => p.Company.Language)
                                         .Where(p => p.Id == applicationUserId)
                                         .First().Company.Language.LanguageId;

                var stringFormat = "yyyyMMddHHmmss";
                var time = DateTime.ParseExact(begTime, stringFormat, CultureInfo.InvariantCulture);
                var fileName = $"{applicationUserId}_{time.ToString(stringFormat)}_{languageId}.mkv";
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, "videos/", fileName);

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
//                _log.Info("Function Video save info finished");

                return Ok();
            }
            catch (Exception e)
            {
//                _log.Fatal("Exception occured {e}");
                return BadRequest(e.Message);
            }


            // _handler.EventRaised(message);
        }
    }
}