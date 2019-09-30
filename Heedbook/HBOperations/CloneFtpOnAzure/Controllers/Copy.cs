using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace CloneFtpOnAzure.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class CopyController : Controller
    {
        private readonly RecordsContext _context;
        // private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
        private IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;


//        private readonly ElasticClient _log;


        public CopyController(IServiceScopeFactory scopeFactory, RecordsContext context, SftpClient sftpClient/*, ElasticClient log*/)
        {
            _scopeFactory = scopeFactory;
            // _handler = handler;
            _context = context;
            _sftpClient = sftpClient;
//            _log = log;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
        public async Task<IActionResult> VideoSave([FromQuery] string folderName, [FromBody] DateTime? begTime)
        {
            
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var oldSettings = new SftpSettings()
                    {
                        Host = "40.112.78.6",
                        Port = 22,
                        UserName = "nkrokhmal",
                        Password = "kloppolk_2018",
                        DestinationPath = "/home/nkrokhmal/storage/",
                        DownloadPath = "/opt/download/"
                    };
                    var sftpCLientOld = new SftpClient(oldSettings, _configuration);
                    var oldPath = await sftpCLientOld.ListDirectoryAsync("");

                    foreach (var sftpFile in oldPath.Where(f => f.Name != "folderName"))
                    {
                        if (sftpFile.IsDirectory)
                        {
                            var files = await sftpCLientOld.ListDirectoryFiles(sftpFile.Name, null, begTime);
                            System.Console.WriteLine(JsonConvert.SerializeObject(files));
                            foreach (var file in files)
                            {
                                using (var stream =
                                    await sftpCLientOld.DownloadFromFtpAsMemoryStreamAsync(sftpFile.Name + "/" + file))
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    await _sftpClient.UploadAsMemoryStreamAsync(stream, sftpFile.Name, file, true);
                                    Console.WriteLine("Uploaded file " + sftpFile.Name + "/" + file);
                                }
                            }
                        }
                    }

                    Console.WriteLine("Upload ended");
                }
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}