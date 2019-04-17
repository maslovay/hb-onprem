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
        private readonly ITokenService _tokenService;
        private readonly RecordsContext _context;


        public CatalogueController(
            IConfiguration config,
            ITokenService tokenService,
            RecordsContext context
            )
        {
            _config = config;
            _tokenService = tokenService;
            _context = context;
        }
        #region Catalogue
        [HttpGet("Country")]
        [SwaggerOperation(Description = "Return all countries. Does not require to transfer a token")]
        public IEnumerable<Country> CountrysGet()
        {             
            return _context.Countrys.ToList();
        }     
        [HttpGet("Role")]
        [SwaggerOperation(Description = "Return all available user roles. Does not require to transfer a token")]
        public IEnumerable<ApplicationRole> RolesGet()
        {             
            return _context.ApplicationRoles.ToList();
        }     
        [HttpGet("Industry")]
        [SwaggerOperation(Description = "Return all industries. Does not require to transfer a token")]
        public IEnumerable<CompanyIndustry> IndustryGet()
        {             
            return _context.CompanyIndustrys.ToList();
        }     
        [HttpGet("Language")]
        [SwaggerOperation(Description = "Return all available languages. Does not require to transfer a token")]
        public IEnumerable<Language> LanguageGet()
        {             
            return _context.Languages.ToList();
        }     
        [HttpGet("PhraseType")]
        [SwaggerOperation(Description = "Return all available phrase types. Does not require to transfer a token")]
        public IEnumerable<PhraseType> PhraseTypeGet()
        {             
            return _context.PhraseTypes.ToList();
        }     
        #endregion
    }
}
