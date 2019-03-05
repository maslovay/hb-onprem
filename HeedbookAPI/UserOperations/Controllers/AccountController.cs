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
using UserOperations.Repository;
using UserOperations.Models;
using UserOperations.Models.AccountViewModels;
using UserOperations.Services;

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
        private readonly IGenericRepository _repository;
        private readonly IConfiguration _config;
        // private readonly ITokenService _tokenService;


        public AccountController(
            IGenericRepository repository,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config
            // ITokenService tokenService
            )
        {
            _repository = repository;
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            // _tokenService = tokenService;
        }

        [HttpPost("register")]
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

                _repository.Create(company);
                _repository.Save();

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

                _repository.Create(user);
                _repository.Save();
                
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    if (await _repository.FindByConditionAsync<Tariff>(item => item.CompanyId == companyId).ToAsyncEnumerable().Count() == 0)
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
                        _repository.Update(company);

                        _repository.Create(tariff);
                        _repository.Create(transaction);                    
                        _repository.Save();
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

        // [AllowAnonymous]
        // [HttpPost("generatetoken")]
        // public async Task<IActionResult> GenerateToken([FromBody]AccountAuthorization message)
        // {
        //     try
        //     {
        //         var user = await _userManager.FindByEmailAsync(message.UserName);
        //         Console.WriteLine(message.UserName);
        //         Console.WriteLine()

        //         if (user != null)
        //         {
        //             var result = await _signInManager.CheckPasswordSignInAsync(user, req.pswd, false);
        //             if (result.Succeeded)
        //             {
        //                 var token = _tokenService.CreateTokenForUser(req.username, req.remember);
        //                 if(token != null)
        //                     return Ok(token);
        //                 else
        //                     return BadRequest("Could not create token");
        //             }
        //         }
        //         return BadRequest("Could not create token");
        //     }
        //     catch
        //     {
        //         return BadRequest("Could not create token");
        //     }
        // }







        [HttpPost("test")]
        public string Test()
        {
            try
            {
            var languageIds = _repository.GetWithInclude<ApplicationUser>(p => 
                    p.Id == Guid.Parse("f3bc2965-cc13-4620-9c67-6b53e5126bab"),
                    link => link.Company).ToList();
            // Console.WriteLine($"{JsonConvert.SerializeObject(languageIds)}");
            var languageId = languageIds.First().Company.LanguageId;
            Console.WriteLine($"{languageId}");

            return languageId.ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}