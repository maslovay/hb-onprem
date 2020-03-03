using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Services;
using UserOperations.Utils;


namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class TabletAppInfoController : Controller
    {
        private readonly TabletAppInfoService _tabletAppInfoService;
        
        public TabletAppInfoController( 
            TabletAppInfoService tabletAppInfoService)
        {
            _tabletAppInfoService = tabletAppInfoService;
        }

        [HttpGet("[action]/{version}")]
        [SwaggerOperation(Summary = "Added version in DB TabletAppInfo")]
        [SwaggerResponse(400, "This version already exist in DB/ Exception occured", typeof(string))]
        [SwaggerResponse(200, "Succesfully added in DB", typeof(string))]
        public object AddCurrentTabletAppVersion([FromRoute]string version) =>
            _tabletAppInfoService.AddCurrentTabletAppVersion(version);    
        
        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Get Latest version of TabletApp")]
        [SwaggerResponse(400, "No version info/ Exception occured", typeof(string))]
        [SwaggerResponse(200, "Return latest version of TabletApp", typeof(string))]
        public object GetCurrentTabletAppVersion() =>
            _tabletAppInfoService.GetCurrentTabletAppVersion();
    }
}