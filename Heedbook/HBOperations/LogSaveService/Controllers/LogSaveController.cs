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

namespace LogSave.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class LogSaveController : Controller
    {
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        public LogSaveController(RecordsContext context, SftpClient sftpClient)
        {
            _context = context;
            _sftpClient = sftpClient;
        }
        [HttpPost]
        [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
        public async Task<IActionResult> LogSave([FromForm] IFormFile file)
        {
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