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
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Cryptography;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        // private readonly ElasticClient _log;

        public PaymentController(
            ILoginService loginService,
            RecordsContext context
            // ElasticClient log
            )
        {
            _loginService = loginService;
            _context = context;
            // _log = log;
        }

        [HttpGet("Tariff")]
        [SwaggerOperation(Summary = "Return tariffs and Transaction", Description = "Return tariffs and Transaction")]
        public async Task<IActionResult> TariffGet([FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("Payment/Tariff GET started"); 
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var tariffs = _context.Tariffs.Include(x => x.Transactions).Where(x => x.CompanyId == companyId).OrderByDescending(x => x.CreationDate).ToList();
                // _log.Info("Payment/Tariff GET finished"); 
                return Ok(tariffs);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPost("CheckoutResponse")]
        [SwaggerOperation(Summary = "Get 2checkout responce. Create payment", Description = "Get 2checkout responce about payment (redirect from wantad, save payment and tariff statuses")]
        public async Task<IActionResult> CheckoutResponsePost([FromBody, SwaggerParameter("2checkout responce", Required = true)] string contentReq)
        {
            try
            {
                // _log.Info("Payment/CheckoutResponse started"); 
                Dictionary<string, string> dict = contentReq.Split('&').Select(s => s.Split('=')).ToDictionary(a => a[0], a => a[1]);
                //compare key (md5_hash) getting by 2Checkout
                //string secretWord = "ZTc5ZjYyNDEtZWVkMi00OTllLWI1ZmYtYjQ0Yjc2OWE4ZTk4"; //sandbox
                string secretWord = "MTAzNDM1ZjctYjk1Mi00OTQ5LWI5NDktMjY1NzAxOGYxYzU2";  //2Chekout
                string key = dict["key"];
                string sid = dict["sid"];
                string order_number = dict["order_number"];
                string total = dict["total"];
                string source = secretWord + sid + order_number + total;

                using (MD5 md5Hash = MD5.Create())
                {
                    byte[] input = Encoding.UTF8.GetBytes(source);
                    byte[] hash = md5Hash.ComputeHash(input);
                    string hashOfInput = BitConverter.ToString(hash).Replace("-", "");
                    StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                    if (0 == comparer.Compare(hashOfInput, key))
                    {
                        return BadRequest("Error hash");
                    }
                }

                //change TransactionPaymentInvoiceId in Payments
                Guid.TryParse(dict["payment_id"], out var payment_id);
                var paymentEntity = _context.Transactions.OrderByDescending(p => p.TransactionId == payment_id).FirstOrDefault();
                if (paymentEntity == null)
                {
                    return BadRequest("Error payment");
                }
                paymentEntity.OrderId = dict["invoice_id"];
                paymentEntity.StatusId = 3;
                paymentEntity.PaymentDate = DateTime.UtcNow;
                paymentEntity.Tariff.StatusId = 3;
                _context.SaveChanges();

                //create response
                var response = new HttpResponseMessage(HttpStatusCode.MovedPermanently);
                response.Headers.Location = new System.Uri("https://hbserviceplan-onprem.azurewebsites.net/company");
                // _log.Info("Payment/CheckoutResponse finished"); 
                return Ok(response);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }
    }
}