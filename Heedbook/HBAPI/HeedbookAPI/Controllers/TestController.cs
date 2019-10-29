using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData;
using UserOperations.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using Swashbuckle.AspNetCore.Annotations;
using HBLib.Utils;
using System.Reflection;

namespace UserOperations.Controllers.Test
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly IDBOperations _dbOperation;
        private readonly IRequestFilters _requestFilters;
        // private readonly ElasticClient _log;
        private readonly SftpClient _sftpClient;

        public TestController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            IDBOperations dbOperation,
            IRequestFilters requestFilters,
            SftpClient sftpClient
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _sftpClient = sftpClient;
        }


        [HttpGet("Origin")]
        [SwaggerOperation(Description = "Return collection of dialogues from dialogue phrases by filters (one page)")]
        public IActionResult DialoguePaginatedGet([FromQuery(Name = "begTime")] string beg,
                                           [FromQuery(Name = "endTime")] string end,
                                           [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                           [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                           [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                           [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                           [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                           [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,

                                           [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization,
                                           [FromQuery(Name = "limit")] int limit = 10,
                                           [FromQuery(Name = "page")] int page = 0,
                                           [FromQuery(Name = "orderBy")] string orderBy = "BegTime",
                                           [FromQuery(Name = "orderDirection")] int orderDirection = 0)
        {
          //  try
            {
                // _log.Info("User/Dialogue GET started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);


                var dialogues = _context.Dialogues
                .Include(p => p.DialogueHint)
                .Where(p =>
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == 3 &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)) &&
                    (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)))
                )
                .Select(p => new
                {
                    p.DialogueId,
                    Avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _sftpClient.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                    p.ApplicationUserId,
                    p.ApplicationUser.FullName,
                    DialogueHints = p.DialogueHint.Count() != 0 ? "YES" : null,
                    p.BegTime,
                    p.EndTime,
                    p.CreationTime,
                    p.Comment,
                    p.SysVersion,
                    p.StatusId,
                    p.InStatistic,
                    p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                }).ToList();



                ////---PAGINATION---
                var pageCount = (int)Math.Ceiling((double)dialogues.Count() / limit);//---round to the bigger 

                Type dialogueType = dialogues.First().GetType();
                PropertyInfo prop = dialogueType.GetProperty(orderBy);
                if (orderDirection == 0)
                {
                    var dialoguesList = dialogues.OrderBy(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { dialoguesList, pageCount, orderBy, limit, page });
                }
                else
                {
                    var dialoguesList = dialogues.OrderByDescending(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { dialoguesList, pageCount, orderBy, limit, page });
                }
                // _log.Info("User/Dialogue GET finished");
            }
            //catch (Exception e)
            //{
            //    // _log.Fatal($"Exception occurred {e}");
            //    return BadRequest(e.Message);
            //}
        }

        [HttpGet("Test")]
        [SwaggerOperation(Description = "Return collection of dialogues from dialogue phrases by filters (one page)")]
        public IActionResult DialoguePaginatedTestGet([FromQuery(Name = "begTime")] string beg,
                                           [FromQuery(Name = "endTime")] string end,
                                           [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                           [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                           [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                           [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                           [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                           [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,

                                           [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization,
                                           [FromQuery(Name = "limit")] int limit = 10,
                                           [FromQuery(Name = "page")] int page = 0,
                                           [FromQuery(Name = "orderBy")] string orderBy = "BegTime",
                                           [FromQuery(Name = "orderDirection")] int orderDirection = 0)
        {
          //  try
            {
                // _log.Info("User/Dialogue GET started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);


                var dialogues = _context.Dialogues
                .Include(p => p.DialogueHint)
                .Where(p =>
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == 3 &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)) &&
                    (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)))
                )
                .Select(p => new
                {
                    p.DialogueId,
                    Avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _sftpClient.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                    p.ApplicationUserId,
                    p.ApplicationUser.FullName,
                    DialogueHints = p.DialogueHint.Count() != 0 ? "YES" : null,
                    p.BegTime,
                    p.EndTime,
                    p.CreationTime,
                    p.Comment,
                    p.SysVersion,
                    p.StatusId,
                    p.InStatistic,
                    p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                });



                ////---PAGINATION---
                var pageCount = (int)Math.Ceiling((double)dialogues.Count() / limit);//---round to the bigger 

                Type dialogueType = dialogues.First().GetType();
                PropertyInfo prop = dialogueType.GetProperty(orderBy);
                if (orderDirection == 0)
                {
                    var dialoguesList = dialogues.OrderBy(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { dialoguesList, pageCount, orderBy, limit, page });
                }
                else
                {
                    var dialoguesList = dialogues.OrderByDescending(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { dialoguesList, pageCount, orderBy, limit, page });
                }
                // _log.Info("User/Dialogue GET finished");
            }
            //catch (Exception e)
            //{
            //    // _log.Fatal($"Exception occurred {e}");
            //    return BadRequest(e.Message);
            //}
        }

    }
}
