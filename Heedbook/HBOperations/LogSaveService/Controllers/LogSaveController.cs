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
        public LogSaveController(RecordsContext context)
        {
            _context = context;
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
                        var path = $"/var/log/saved_logs/{file.FileName}";
                        System.Console.WriteLine(path);
                        using(FileStream newFile = new FileStream(path, FileMode.Create, FileAccess.Write))
                        {
                            byte[] bytes = new byte[fileStream.Length];
                            fileStream.Read(bytes, 0, (int)fileStream.Length);
                            newFile.Write(bytes, 0, (int)fileStream.Length);
                        }
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
