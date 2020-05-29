using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Utils;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
  //  [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    [ControllerExceptionFilter]
    public class VideoSaveInfoController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
        //private readonly ElasticClient _log;


        public VideoSaveInfoController(INotificationHandler handler, IGenericRepository repository, SftpClient sftpClient /*, ElasticClient log*/)
        {
            _handler = handler;
            _repository = repository;
            _sftpClient = sftpClient;
           // _log = log;

        }

        [HttpGet]
        [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
        public async Task<FileVideo> VideoSave(
            [FromQuery] Guid deviceId,
            [FromQuery] String begTime,
            [FromQuery] Double? duration,
            [FromQuery] string applicationUserId,
            [FromQuery] String endTime = null)
        {
            try
            {
                duration = duration == null ? 15 : duration;
                var languageId = _repository.GetAsQueryable<Device>()
                    .Where(p => p.DeviceId == deviceId)
                    .Select( x => x.Company.Language.LanguageId).First();
                var isExtended = _repository.GetAsQueryable<Device>()
                    .Include(p => p.Company)
                    .Where(p => p.DeviceId == deviceId).FirstOrDefault().Company.IsExtended;
                            
                Guid? userId = null;
                try
                {
                    userId = Guid.Parse(applicationUserId);
                }
                catch {}
                var stringFormat = "yyyyMMddHHmmss";
                var timeBeg = DateTime.ParseExact(begTime, stringFormat, CultureInfo.InvariantCulture);
                var timeEnd = endTime != null ? DateTime.ParseExact(endTime, stringFormat, CultureInfo.InvariantCulture): timeBeg.AddSeconds((double)duration);
                var fileName = $"{userId?? Guid.Empty}_{deviceId}_{timeBeg.ToString(stringFormat)}_{languageId}.mkv";

                var videoIntersectVideosAny = _repository.GetAsQueryable<FileVideo>()
                    .Where(p => p.DeviceId == deviceId
                        && ((p.BegTime <= timeBeg
                                && p.EndTime > timeBeg
                                && p.EndTime < timeEnd) 
                            || (p.BegTime < timeEnd
                                && p.BegTime > timeBeg
                                && p.EndTime >= timeEnd)
                            || (p.BegTime >= timeBeg
                                && p.EndTime <= timeEnd)
                            || (p.BegTime < timeBeg
                                && p.EndTime > timeEnd)))
                    .Any();
                var videoFile = new FileVideo
                {
                    ApplicationUserId = userId,
                    DeviceId = deviceId,
                    BegTime = timeBeg,
                    CreationTime = DateTime.UtcNow,
                    Duration = duration,
                    EndTime = timeEnd,
                    FileContainer = "videos",
                    FileExist = await _sftpClient.IsFileExistsAsync($"videos/{fileName}"),
                    FileName = fileName,
                    FileVideoId = Guid.NewGuid(),
                    StatusId = 6
                };
                if (videoIntersectVideosAny)
                {
                    videoFile.StatusId = 8;
                }    
                _repository.Create<FileVideo>(videoFile);
                _repository.Save();

                if (videoFile.FileExist && isExtended)
                {
                    var message = new FramesFromVideoRun();
                    message.Path = $"videos/{fileName}";
//                    _log.Info($"Sending message {JsonConvert.SerializeObject(message)}");
                    _handler.EventRaised(message);
                }
                else
                {
//                    _log.Error($"No such file videos/{fileName}");
                }
//                _log.Info("Function Video save info finished");
                return videoFile;
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occured while executing Video save info {e}");
                throw e;
            }
        }
    }
}