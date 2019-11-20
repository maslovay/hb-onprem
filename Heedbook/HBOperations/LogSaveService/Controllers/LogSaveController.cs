using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class LogSaveController : Controller
    {
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
//        private readonly ElasticClient _log;


        public LogSaveController(INotificationHandler handler, RecordsContext context, SftpClient sftpClient/*, ElasticClient log*/)
        {
            _handler = handler;
            _context = context;
            _sftpClient = sftpClient;
//            _log = log;
        }
        [HttpPost]
        [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
        public async Task<IActionResult> LogSave([FromForm] IFormCollection formData)
        {
            try
            {  
                var file = formData.Files.FirstOrDefault();
                if(file != null)
                {                       
                    var fileStream = file.OpenReadStream();
                    if(fileStream.Length != 0)
                    {                   
                        var path = $"/var/log/{file.FileName}";
                        using(FileStream newFile = new FileStream(path, FileMode.Create, FileAccess.Write))
                        {
                            byte[] bytes = new byte[fileStream.Length];
                            fileStream.Read(bytes, 0, (int)fileStream.Length);
                            newFile.Write(bytes, 0, (int)fileStream.Length);
                        }
                    }
                }
                else
                    return BadRequest("file not exist");
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}