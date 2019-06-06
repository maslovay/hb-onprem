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
    public class VideoSaveInfoController : Controller
    {
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
        private readonly ElasticClient _log;


        public VideoSaveInfoController(INotificationHandler handler, RecordsContext context, SftpClient sftpClient, ElasticClient log)
        {
            _handler = handler;
            _context = context;
            _sftpClient = sftpClient;
            _log = log;

        }

        [HttpGet]
        [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
        public async Task<IActionResult> VideoSave([FromQuery] Guid applicationUserId,
            [FromQuery] String begTime,
            [FromQuery] Double? duration,
            [FromQuery] String endTime = null)
        {
            try
            {   
                _log.Info("Function Video save info started");
                duration = duration == null ? 15 : duration;
                var languageId = _context.ApplicationUsers
                                         .Include(p => p.Company)
                                         .Include(p => p.Company.Language)
                                         .Where(p => p.Id == applicationUserId)
                                         .First().Company.Language.LanguageId;

                var stringFormat = "yyyyMMddHHmmss";
                var time = DateTime.ParseExact(begTime, stringFormat, CultureInfo.InvariantCulture);
                var timeEnd = endTime != null ? DateTime.ParseExact(endTime, stringFormat, CultureInfo.InvariantCulture): time.AddSeconds((double)duration);
                var fileName = $"{applicationUserId}_{time.ToString(stringFormat)}_{languageId}.mkv";

                var videoFile = new FileVideo{
                    ApplicationUserId = applicationUserId,
                    BegTime = time,
                    CreationTime = DateTime.UtcNow,
                    Duration = duration,
                    EndTime = timeEnd,
                    FileContainer = "videos",
                    FileExist = await _sftpClient.IsFileExistsAsync($"videos/{fileName}"),
                    FileName = fileName,
                    FileVideoId = Guid.NewGuid(),
                    StatusId = 6
                };
                _context.FileVideos.Add(videoFile);
                _context.SaveChanges();

                if (videoFile.FileExist)
                {
                    var message = new FramesFromVideoRun();
                    message.Path = $"videos/{fileName}";
                    _log.Info($"Sending message {JsonConvert.SerializeObject(message)}");
                    _handler.EventRaised(message);
                }
                else
                {
                    _log.Error($"No such file videos/{fileName}");
                }
                _log.Info("Function Video save info finished");
                return Ok();
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured while executing Video save info {e}");
                return BadRequest(e);
            }
        }
    }
}