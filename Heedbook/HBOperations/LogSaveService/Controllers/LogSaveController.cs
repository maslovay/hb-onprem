using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Notifications.Base;
using Swashbuckle.AspNetCore.Annotations;

namespace LogSave.Controllers
{
    [Route("logs/[controller]")]
    [ApiController]
    public class LogSaveController : Controller
    {
        private readonly RecordsContext _context;
        private readonly SftpClient _client;
        public LogSaveController(RecordsContext context, SftpClient client)
        {
            _context = context;
            _client = client;
        }
        [HttpPost]
        [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
        public async Task<IActionResult> LogSave([FromForm] IFormFile file)
        {
            var answer = new Dictionary<string, object>();
            try
            {  
                System.Console.WriteLine($"{file.FileName}");
                //var file = formData.Files.FirstOrDefault();
                if(file != null)
                {                       
                    var fileStream = file.OpenReadStream();
                    if(fileStream.Length != 0)
                    {
                        await _client.UploadAsMemoryStreamAsync(fileStream, "log/", file.FileName);
                    }
                }
                else
                {
                    answer["success"] = false;
                    answer["error"] = "File not exist";
                    return BadRequest(answer);
                }
                answer["status"] = true;
                return Ok(answer);
            }
            catch (Exception e)
            {
                answer["success"] = false;
                answer["error"] = e.Message;
                return BadRequest(answer);
            }
        }
    }
}
