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
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.AccountModels;
using HBData;


using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;



namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly RecordsContext _context;


        public HelpController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ITokenService tokenService,
            RecordsContext context
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpGet("DatabaseFilling")]
        public string DatabaseFilling()
        {
            var countryId = Guid.NewGuid();
            var country = new Country{
                CountryId = countryId,
                CountryName = "Russia",
            };
            _context.Countrys.Add(country);
            _context.SaveChanges();

            // add language
            var language = new Language{
                LanguageId = 1,
                LanguageName = "English",
                LanguageLocalName = "English",
                LanguageShortName = "en-us"
            };
            _context.Languages.Add(language);
            _context.SaveChanges();

            language = new Language{
                LanguageId = 2,
                LanguageName = "Russian",
                LanguageLocalName = "Русский",
                LanguageShortName = "ru-RU"
            };
            _context.Languages.Add(language);
            _context.SaveChanges();

            // create company industry
            var companyIndustryId = Guid.NewGuid();
            var companyIndustry = new CompanyIndustry{
                CompanyIndustryId = companyIndustryId,
                CompanyIndustryName = "IT",
                CrossSalesIndex = 100,
                LoadIndex = 100,
                SatisfactionIndex = 100
            };
            _context.CompanyIndustrys.Add(companyIndustry);
            _context.SaveChanges();

            // create new corporation
            var corporationId = Guid.NewGuid(); 
            var corp = new Corporation{
                Id = corporationId,
                Name = "Heedbook" 
            };
            _context.Corporations.Add(corp);
            _context.SaveChanges();

            // add statuss
            List<string> statuses = new List<string>(new string[] { "Online", "Offline", "Active", "Disabled", "Inactive", "InProgress", "Finished", "Error", "Pending disabled", "Trial", "AutoActive", "AutoFinished", "AutoError" });
            
            
            for (int i = 1; i < statuses.Count() + 1; i++)
            {   
                var status = new Status{
                    StatusId = i,
                    StatusName = statuses[i]
                };
                _context.Statuss.Add(status);
                _context.SaveChanges();
            }
            return "OK";
        }

        // [HttpGet("test")]
        // public string test()
        // {
        //     try
        //     {
        //         var videos = Json

        //         return "OK";
        //     }
        //     catch (Exception e)
        //     {
        //         return e.ToString();
        //     }

        // }
    }
}