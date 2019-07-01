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

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using HBLib.Utils;
using HBLib;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogueController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly ElasticClient _log;


        public CatalogueController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _log = log;
        }

        [HttpGet("Country")]
        [SwaggerOperation(Description = "Return all countries. Does not require to transfer a token")]
        public IEnumerable<Country> CountrysGet()
        {
            // _log.Info("Catalogue/Country GET");
            return _context.Countrys.ToList();
        }
        [HttpGet("Role")]
        [SwaggerOperation(Description = "Return all available user roles. Does not require to transfer a token")]
        public IEnumerable<ApplicationRole> RolesGet()
        {
            // _log.Info("Catalogue/Role GET");
            return _context.ApplicationRoles.ToList();
        }

        [HttpGet("WorkerType")]
        [SwaggerOperation(Summary = "Return worker types", Description = "Return all available worker types for company with id ('Кассир','Кредитный менеджер' и др). Require to transfer a token")]
        [SwaggerResponse(200, "Content", typeof(WorkerType))]
        public IEnumerable<object> WorkerTypeGet([FromHeader] string Authorization)
        {
            // _log.Info("Catalogue/WorkerType GET");
            if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                return null;
            var companyId = Guid.Parse(userClaims["companyId"]);
            return _context.WorkerTypes.Where(p => p.CompanyId == companyId).Select(p => new { p.WorkerTypeId, p.WorkerTypeName }).ToList();
        }

        [HttpGet("Industry")]
        [SwaggerOperation(Description = "Return all industries. Does not require to transfer a token")]
        public IEnumerable<CompanyIndustry> IndustryGet()
        {
            // _log.Info("Catalogue/Industry GET");
            return _context.CompanyIndustrys.ToList();
        }
        [HttpGet("Language")]
        [SwaggerOperation(Description = "Return all available languages. Does not require to transfer a token")]
        public IEnumerable<Language> LanguageGet()
        {
            // _log.Info("Catalogue/Language GET");
            return _context.Languages.ToList();
        }
        [HttpGet("PhraseType")]
        [SwaggerOperation(Description = "Return all available phrase types. Does not require to transfer a token")]
        public IEnumerable<PhraseType> PhraseTypeGet()
        {
            _log.Info("Catalogue/PhraseType GET");
            return _context.PhraseTypes.ToList();
        }
    }
}
