using System;
using System.Linq;
using HBData.Models;
using HBData.Repository;
using Microsoft.AspNetCore.Mvc;



namespace UserOperations.Services
{
    public class TabletAppInfoService
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
            //if(!_loginService.IsAdmin())
            //{
            //    var ex = new Exception("Requires ADMIN role!");
            //    throw ex;
            //}
            
            if ( _repository.GetAsQueryable<TabletAppInfo>().Any( t => string.Equals(t.TabletAppVersion, version, StringComparison.CurrentCultureIgnoreCase) ) )
                return new BadRequestObjectResult("This version already exists!");
            
            var newVersion = new TabletAppInfo()
            {
                ReleaseDate = DateTime.Now,
                TabletAppVersion = version
            };

            _repository.CreateAsync<TabletAppInfo>(newVersion);
            _repository.Save();

            return newVersion;
        }
        public object GetCurrentTabletAppVersion()
        {
            var currentRelease = _repository.GetAsQueryable<TabletAppInfo>().OrderByDescending(t => t.ReleaseDate).FirstOrDefault();
            if (currentRelease == null)
                return "No version info!";
            return currentRelease;
        }
    }
}