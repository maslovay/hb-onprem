using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractFramesFromVideo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly SftpClient _client;

        private readonly IGenericRepository _repository;
        
        public ValuesController(SftpClient client,
            IServiceScopeFactory factory)
        {
            _client = client;
            var scope = factory.CreateScope();
            _repository = scope.ServiceProvider.GetService<IGenericRepository>();
        }

        // GET api/values
        [HttpGet]
        //public async Task<ActionResult <string>> GetAsync([FromQuery] string videoBlobName)
        public async Task Get([FromQuery] string videoBlobName)
        {

            //}   
        }
    }
}