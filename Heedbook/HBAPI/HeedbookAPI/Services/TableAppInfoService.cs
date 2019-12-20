using System;
using System.Linq;
using HBData.Models;
using HBData.Repository;
using Microsoft.AspNetCore.Mvc;



namespace UserOperations.Services
{
    public class TabletAppInfoService : Controller
    {
        private readonly LoginService _loginService;
        private readonly IGenericRepository _repository;
        
        public TabletAppInfoService(
            LoginService loginService,
            IGenericRepository repository)
        {
            _loginService = loginService;
            _repository = repository;
        }
        public object AddCurrentTabletAppVersion([FromRoute]string version)
        {
            if(_loginService.GetCurrentRoleName().ToUpper() != "ADMIN")
                return "Requires ADMIN role!";
            
            if ( _repository.GetAsQueryable<TabletAppInfo>().Any( t => string.Equals(t.TabletAppVersion, version, StringComparison.CurrentCultureIgnoreCase) ) )
                return new BadRequestObjectResult("This version already exists!");
            
            var newVersion = new TabletAppInfo()
            {
                ReleaseDate = DateTime.Now,
                TabletAppVersion = version
            };

            _repository.Create<TabletAppInfo>(newVersion);
            _repository.Save();

            return Ok(newVersion);
        }
        public object GetCurrentTabletAppVersion()
        {
            var currentRelease = _repository.GetAsQueryable<TabletAppInfo>().OrderByDescending(t => t.ReleaseDate).FirstOrDefault();
            System.Console.WriteLine($"currentRelease is null: {currentRelease is null}");
            if (currentRelease == null)
                return NotFound("No version info!");
            return currentRelease;
        }
    }
}