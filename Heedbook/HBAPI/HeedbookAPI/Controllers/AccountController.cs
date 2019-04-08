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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly RecordsContext _context;


        public AccountController(
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

        [HttpPost("Register")]
        public async Task<string>  UserRegister([FromBody] UserRegister message)
        {
            try
            { 
                var companyId = Guid.NewGuid();
                var company = new Company{
                    CompanyId = companyId,
                    CompanyIndustryId = message.CompanyIndustryId,
                    CompanyName = message.CompanyName,
                    LanguageId = message.LanguageId,
                    CreationDate = DateTime.UtcNow,
                    CountryId = message.CountryId,
                    StatusId = 5
                };

                _context.Companys.Add(company);

                var user = new ApplicationUser { 
                    UserName = message.Email,
                    Email = message.Email,
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    CreationDate = DateTime.UtcNow,
                    FullName = message.FullName,
                    StatusId = 3};

                var result = await _userManager.CreateAsync(user, message.Password);
                await _userManager.AddToRoleAsync(user, "Manager");

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    if (_context.Tariffs.Where(item => item.CompanyId == companyId).ToList().Count() == 0)
                    {
                        Tariff tariff = new Tariff
                        {
                            TariffId = Guid.NewGuid(),
                            TotalRate = 0,
                            CompanyId = companyId,
                            CreationDate = DateTime.UtcNow,
                            CustomerKey = "",
                            EmployeeNo = 2,
                            ExpirationDate = DateTime.UtcNow.AddDays(5),
                            isMonthly = false,
                            Rebillid = "",
                            StatusId = 10
                        };

                        Transaction transaction = new Transaction
                        {
                            TransactionId = Guid.NewGuid(),
                            Amount = 0,
                            OrderId = "",
                            PaymentId = "",
                            TariffId = tariff.TariffId,
                            StatusId = 7,
                            PaymentDate = DateTime.UtcNow,
                            TransactionComment = "TRIAL TARIFF;FAKE TRANSACTION"
                        };

                        company.StatusId = 3;
                        
                        _context.Tariffs.Add(tariff);
                        _context.Transactions.Add(transaction);
                        var ids = _context.ApplicationUsers.Where(p => p.Id == user.Id).ToList();
                        _context.SaveChanges();
                        _context.Dispose();
                        // _signInManager.
                    }
                    else
                    {
                        
                    }
                }
                return "Ok";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        [AllowAnonymous]
        [HttpPost("GenerateToken")]
        public async Task<IActionResult> GenerateToken([FromBody]AccountAuthorization message)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(message.UserName);
                Console.WriteLine(message.UserName);
                System.Console.WriteLine(user == null);  

                if (user != null)
                {
                    var result = await _signInManager.CheckPasswordSignInAsync(user, message.Password, false);
                    System.Console.WriteLine("result of checking " + result.Succeeded);
                    if (result.Succeeded)
                    {
                        var token = _tokenService.CreateTokenForUser(message.UserName, message.Remember);
                        System.Console.WriteLine(token);
                        if(token != null)
                            return Ok(token);
                        else
                            return BadRequest("Could not create token");
                    }
                }
                return BadRequest("Could not create token");
            }
            catch (Exception e)
            {
                return BadRequest($"Could not create token {e}");
            }
        }



        // [HttpPost("test")]
        // public string Test()
        // {
        //     return "OK";
        // }
    }
}