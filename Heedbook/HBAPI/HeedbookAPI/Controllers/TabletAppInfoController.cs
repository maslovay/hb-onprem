using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HBData;
using HBData.Models;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;


namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TabletAppInfoController : Controller
    {
        private readonly RecordsContext _context;
        private readonly LoginService _loginService;
        
        public TabletAppInfoController( RecordsContext context, LoginService loginService )
        {
            _context = context;
            _loginService = loginService;
        }

        [HttpGet("[action]/{version}")]
        public IActionResult AddCurrentTabletAppVersion([FromRoute]string version, [FromHeader] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                return BadRequest("Token wrong");
            if (userClaims["role"].ToUpper() != "ADMIN")
                return BadRequest("Requires ADMIN role!");
            
            if ( _context.TabletAppInfos.Any( t => string.Equals(t.TabletAppVersion, version, StringComparison.CurrentCultureIgnoreCase) ) )
                return new BadRequestObjectResult("This version already exists!");
            
            var newVersion = new TabletAppInfo()
            {
                ReleaseDate = DateTime.Now,
                TabletAppVersion = version
            };

            _context.Add(newVersion);
            _context.SaveChanges();

            return Ok(newVersion);
        }
        
        [HttpGet("[action]")]
        public IActionResult GetCurrentTabletAppVersion()
        {
            var currentRelease = _context.TabletAppInfos.OrderByDescending(t => t.ReleaseDate).FirstOrDefault();

            if (currentRelease == null)
                return NotFound("No version info!");

            return Ok(currentRelease);
        }
    }
}