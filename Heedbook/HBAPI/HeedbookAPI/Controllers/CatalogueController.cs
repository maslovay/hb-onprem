using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using UserOperations.AccountModels;
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using HBData;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class CatalogueController : Controller
    {        
        private readonly CatalogueService _catalogueService;

        public CatalogueController(CatalogueService catalogueService)
        {
            _catalogueService = catalogueService;
        }

        [HttpGet("Country")]
        [SwaggerOperation(Description = "Return all countries. Does not require to transfer a token")]
        [AllowAnonymous]
        public IEnumerable<Country> CountrysGet() => 
            _catalogueService.CountrysGet();
        
        [HttpGet("Role")]
        [SwaggerOperation(Description = "Return all available user roles. Does not require to transfer a token")]
        [AllowAnonymous]
        public IEnumerable<ApplicationRole> RolesGet() =>
            _catalogueService.RolesGet();

        [HttpGet("WorkerType")]
        [SwaggerOperation(Summary = "Return worker types", Description = "Return all available worker types for company with id ('Кассир','Кредитный менеджер' и др). Require to transfer a token")]
        [SwaggerResponse(200, "Content", typeof(WorkerType))]
        [AllowAnonymous]
        public IEnumerable<object> WorkerTypeGet([FromHeader] string Authorization) =>
            _catalogueService.WorkerTypeGet(Authorization);

        [HttpGet("Industry")]
        [SwaggerOperation(Description = "Return all industries. Does not require to transfer a token")]
        [AllowAnonymous]
        public IEnumerable<CompanyIndustry> IndustryGet() =>
            _catalogueService.IndustryGet();

        [HttpGet("Language")]
        [SwaggerOperation(Description = "Return all available languages. Does not require to transfer a token")]
        [AllowAnonymous]
        public IEnumerable<Language> LanguageGet() =>
            _catalogueService.LanguageGet();

        [HttpGet("PhraseType")]
        [SwaggerOperation(Description = "Return all available phrase types. Does not require to transfer a token")]
        [AllowAnonymous]
        public IEnumerable<PhraseType> PhraseTypeGet() =>
            _catalogueService.PhraseTypeGet();

        [HttpGet("AlertType")]
        [SwaggerOperation(Description = "Return all available alert types. Does not require to transfer a token")]
        [AllowAnonymous]
        public IEnumerable<AlertType> AlertTypeGet() =>
            _catalogueService.AlertTypeGet();
    }
}
