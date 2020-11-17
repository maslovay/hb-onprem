using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData;
using HBLib.Utils;
using UserOperations.Utils;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Renci.SshNet.Common;
using HBData.Repository;
using UserOperations.Services.Interfaces;
using UserOperations.Utils.Interfaces;
using UserOperations.Models.Get.AnalyticSpeechController;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;
using System.IO;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize(AuthenticationSchemes = "Bearer")]
    // [ControllerExceptionFilter]
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> GenerateReport(string corporationName, string begTime, string endTime)
        {
            try
            {
                var reportName = "Report.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var result = await _reportService.GenerateReport(reportName, corporationName, begTime, endTime);
                if(result == "report generated")
                {
                    var stream = new FileStream(reportName, FileMode.Open);
                    System.IO.File.Delete(reportName);
                    return File(stream, contentType, reportName);
                }
                else
                    return Ok(result);
            }
            catch(Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}
