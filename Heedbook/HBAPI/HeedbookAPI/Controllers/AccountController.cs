using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using HBData.Models.AccountViewModels;
using UserOperations.AccountModels;
using UserOperations.Services;
using System.Collections.Generic;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [ControllerExceptionFilter]
   // [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly AccountService _service;

        public AccountController( AccountService service )
        {
            _service = service;
        }

        [HttpPost("UserRegister")]
        [SwaggerOperation(Summary = "Create user for existed company")]
        [SwaggerResponse(400, "Exception message")]
        [SwaggerResponse(200, "Registred")]
        public async Task<string> UserRegisterForExistedCompany([FromBody,
                        SwaggerParameter("User data", Required = true)]
                        UserRegisterInExistedCompany message)
        {
            await _service.AddUserInExistedCompany(message);
            return "Registred";
        }

        [HttpPost("Register")]
        [SwaggerOperation(Summary = "Create user, company, trial tariff",
            Description = "Create new active company, new active user, add manager role, create new trial Tariff on 5 days/2 employee and new finished Transaction if no exist")]
        [SwaggerResponse(400, "Exception message")]
        [SwaggerResponse(200, "Registred")]
        public async Task<string> UserRegister([FromBody,
                        SwaggerParameter("User and company data", Required = true)]
                        UserRegister message)
        {
            await _service.RegisterNewCompanyAndUser(message);
            return "Registred";
        }


        [HttpPost("GenerateToken")]
        [SwaggerOperation(Summary = "Loggin user", Description = "Loggin for user. Return jwt token.")]
        [SwaggerResponse(400, "The user data is invalid", typeof(string))]
        [SwaggerResponse(200, "JWT token")]
        public string GenerateToken([FromBody,
                        SwaggerParameter("User data", Required = true)]
                        AccountAuthorization message)
            => _service.GenerateToken(message);


        [HttpPost("ChangePassword")]
        [SwaggerOperation(Summary = "Change password", Description = "Change password for user. For loggined user change password with input. If user not loggined receive generated password on email.")]
        [SwaggerResponse(400, "No such user / Exception message", typeof(string))]
        [SwaggerResponse(200, "Password changed")]
        public async Task<string> UserChangePasswordAsync(
                    [FromBody, SwaggerParameter("email required, password only with token")] AccountAuthorization message,
                    [FromHeader, SwaggerParameter("JWT token not required, if need receive new password on email, otherwise, generate new password", Required = false)] string Authorization)
            => await _service.ChangePassword(message, Authorization);

        [HttpPost("ValidateToken")]
        [SwaggerOperation(Summary = "Return true or false")]
        [SwaggerResponse(400, "The user data is invalid", typeof(string))]
        [SwaggerResponse(200, "{\"status\": <bool>}")]
        public async Task<Dictionary<string,bool>> ValidateToken([FromHeader, SwaggerParameter("JWT token required", Required = true)] string Authorization)
         => await _service.ValidateToken(Authorization);


        [HttpPost("ChangePasswordOnDefault")]
        [SwaggerOperation(Summary = "For own use", Description = "Change password for user on Test_User12345")]
        [SwaggerResponse(400, "No such user / Exception message", typeof(string))]
        [SwaggerResponse(200, "Password changed")]
        public async Task<string> UserChangePasswordOnDefaultAsync( [FromBody] string email )
             => await _service.ChangePasswordOnDefault(email);


        [HttpDelete("RemoveUserCompany")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [SwaggerOperation(Summary = "Delete user, company, trial tariff - only for developers", Description = "Delete current user Company with all users")]
        public async Task<string> AccountDelete([FromQuery,
                        SwaggerParameter("user email", Required = true)]
                        string email)
            => await _service.DeleteCompany(email);

        [HttpDelete("RemoveUser")]
        [SwaggerOperation(Summary = "Delete user", Description = "Delete user with current Email")]
        public async Task UserDelete([FromQuery,
                        SwaggerParameter("user email", Required = true)]
                        string email)
          =>  _service.DeleteUser(email);

        [HttpGet("[action]")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [SwaggerOperation(Summary = "Add CompanyPhrases from Excel table - only for developers", Description = "Add CompanyPhrases in DB form Excel table, when api runned locally")]
        public void AddCompanyDictionary(string fileName)=>  _service.AddPhrasesFromExcel(fileName);


        [HttpPost("EmptyToken")]
        [SwaggerOperation(Summary = "get empty JWT Token - only for developers", Description = "")]
        public async Task<string> EmptyToken()=>await _service.CreateEmptyToken();
    }
}