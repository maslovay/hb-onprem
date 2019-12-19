using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using HBData.Models.AccountViewModels;
using UserOperations.AccountModels;
using UserOperations.Providers;

namespace UserOperations.Controllers
{
    [ControllerExceptionFilter]
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly AccountService _service;

        public AccountController( AccountService service )
        {
            _service = service;
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
        [SwaggerOperation(Summary = "Loggin user", Description = "Loggin for user. Return jwt token. Save errors passwords history (Block user)")]
        [SwaggerResponse(400, "The user data is invalid", typeof(string))]
        [SwaggerResponse(200, "JWT token")]
        public string GenerateToken([FromBody,
                        SwaggerParameter("User data", Required = true)]
                        AccountAuthorization message)
            => _service.GenerateToken(message);


        [HttpPost("ChangePassword")]
        [SwaggerOperation(Summary = "two cases", Description = "Change password for user. Receive email. Receive new password for loggined user(with token) or send new password on email")]
        [SwaggerResponse(400, "No such user / Exception message", typeof(string))]
        [SwaggerResponse(200, "Password changed")]
        public async Task<string> UserChangePasswordAsync(
                    [FromBody, SwaggerParameter("email required, password only with token")] AccountAuthorization message,
                    [FromHeader, SwaggerParameter("JWT token not required, if exist receive new password, if not - generate new password", Required = false)] string Authorization)
            => await _service.ChangePassword(message);


        [HttpPost("ChangePasswordOnDefault")]
        [SwaggerOperation(Summary = "For own use", Description = "Change password for user on Test_User12345")]
        [SwaggerResponse(400, "No such user / Exception message", typeof(string))]
        [SwaggerResponse(200, "Password changed")]
        public async Task<string> UserChangePasswordOnDefaultAsync( [FromBody] string email )
             => await _service.ChangePasswordOnDefault(email);


        [HttpDelete("Remove")]
        [SwaggerOperation(Summary = "Delete user, company, trial tariff - only for developers")]
        public async Task<string> AccountDelete([FromQuery,
                        SwaggerParameter("user email", Required = true)]
                        string email)
            => await _service.DeleteCompany(email);


        [HttpGet("[action]")]
        public void AddCompanyDictionary(string fileName)=>  _service.AddPhrasesFromExcel(fileName);
    }
}