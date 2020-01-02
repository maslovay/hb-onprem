using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HBData.Models;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [ControllerExceptionFilter]
    public class CatalogueController : Controller
    {        
        private readonly CatalogueService _catalogueService;

        public CatalogueController(CatalogueService catalogueService)
        {
            _catalogueService = catalogueService;
        }

        [HttpGet("Country")]
        [SwaggerOperation(Description = "Return all countries. Does not require to transfer a token")]
        public IEnumerable<Country> CountrysGet() => 
            _catalogueService.CountrysGet();
        
        [HttpGet("Role")]
        [SwaggerOperation(Description = "Return all available user roles. Does not require to transfer a token")]
        public IEnumerable<ApplicationRole> RolesGet() =>
            _catalogueService.RolesGet();

        [HttpGet("DeviceType")]
        [SwaggerOperation(Summary = "Return device types", Description = "Return all available device types")]
        [SwaggerResponse(200, "Content", typeof(DeviceType))]
        public IEnumerable<object> DeviceTypeGet() =>
            _catalogueService.DeviceTypeGet();

        [HttpGet("Industry")]
        [SwaggerOperation(Description = "Return all industries. Does not require to transfer a token")]
        public IEnumerable<CompanyIndustry> IndustryGet() =>
            _catalogueService.IndustryGet();

        [HttpGet("Language")]
        [SwaggerOperation(Description = "Return all available languages. Does not require to transfer a token")]
        public IEnumerable<Language> LanguageGet() =>
            _catalogueService.LanguageGet();

        [HttpGet("PhraseType")]
        [SwaggerOperation(Description = "Return all available phrase types. Does not require to transfer a token")]
        public IEnumerable<PhraseType> PhraseTypeGet() =>
            _catalogueService.PhraseTypeGet();

        [HttpGet("AlertType")]
        [SwaggerOperation(Description = "Return all available alert types. Does not require to transfer a token")]
        public IEnumerable<AlertType> AlertTypeGet() =>
            _catalogueService.AlertTypeGet();
    }
}
