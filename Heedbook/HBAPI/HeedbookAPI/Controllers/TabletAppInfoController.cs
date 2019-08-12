using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Models.AccountViewModels;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.AccountModels;
using UserOperations.Services;


namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TabletAppInfoController : Controller
    {
        private readonly RecordsContext _context;
        
        public TabletAppInfoController( RecordsContext context )
        {
            _context = context;
        }

        [HttpGet("[action]/{version}")]
        public IActionResult AddCurrentTabletAppVersion(string version)
        {
            if ( _context.TabletAppInfos.Any( t => String.Equals(t.TabletAppVersion, version, StringComparison.CurrentCultureIgnoreCase) ) )
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