using System;
using System.IO;
using System.Linq;
using System.Net;
using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.Text;
using ApiPerformance.Services;
using System.Threading.Tasks;

namespace ApiPerformance.Controllers
{
    [Route("performance/[controller]")]
    [ApiController]
    public class ApiPerformanceController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;
        private readonly ApiPerformanceService _service;        
        public ApiPerformanceController(RecordsContext context, 
            IGenericRepository repository,
            ApiPerformanceService service)
        {
            _context = context;
            _repository = repository;
            _service = service;
        }

        [HttpGet("ApiWorkingTimeReport")]
        [SwaggerResponse(200, "Report constructed")]
        public async Task<IActionResult> ApiWorkingTimeReport([FromQuery] int numberOfAttempts)
        {       
            try
            {
                var fileName = "ApiWorkingTimeReport.xlsx";
                var fileType =  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var result = await _service.CheckAPIWorkTime(fileName, numberOfAttempts);
                if(result == "report generated")
                {
                    var stream = new FileStream(fileName, FileMode.Open);
                    System.IO.File.Delete(fileName);
                    return File(stream, fileType, fileName);
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


      