using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Services;
using HBData.Models;
using UserOperations.AccountModels;
using UserOperations.Models;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [ControllerExceptionFilter]
    public class DeviceController : Controller
    {
        private readonly DeviceService _deviceService;
        public DeviceController( DeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpPost("GenerateToken")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Generate token for device", Description = "JWT token")]
        [SwaggerResponse(200, "JWT token", typeof(string))]
        public async Task<string> GenerateToken([FromBody] string code)
           => await _deviceService.GenerateToken(code);

        [HttpGet("EmployeeList")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [SwaggerOperation(Summary = "list of users", Description = "get users and session status on devices")]
        [SwaggerResponse(200, "Users[]", typeof(List<GetUsersSessions>))]
        public async Task<ICollection<GetUsersSessions>> UsersGet()
           => await _deviceService.GetAllUsersSessions();

        [HttpGet("List")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [SwaggerOperation(Summary = "list of devices", Description = "")]
        [SwaggerResponse(200, "Devices[]", typeof(List<GetDevice>))]
        public async Task<ICollection<GetDevice>> DevicesGet( [FromQuery(Name = "companyId[]")] List<Guid> companyIds) 
            => await _deviceService.GetAll(companyIds);

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [SwaggerOperation(Summary = "add new device for company", Description = "")]
        [SwaggerResponse(200, "Saved", typeof(string))]
        public async Task<Device> DeviceCreate([FromBody] PostDevice device)
            => await _deviceService.Create(device);

        [HttpPut]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [SwaggerOperation(Summary = "set device code, type, name, status", Description = "")]
        [SwaggerResponse(200, "Saved", typeof(string))]
        public async Task<string> DeviceUpdate([FromBody] PutDevice device)
            => await _deviceService.Update(device);

        [HttpDelete]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [SwaggerOperation(Summary = "delete device or set inactive", Description = "real deletion, provided that there is no any dialogue in the database from this device")]
        [SwaggerResponse(200, "Deleted", typeof(string))]
        public async Task<string> DeviceDelete([FromQuery] Guid deviceId)
            => await _deviceService.Delete(deviceId);
    }
}
