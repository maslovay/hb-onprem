using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using UserOperations.Services;
using HBData;
using Newtonsoft.Json;
using HBLib.Utils;
using UserOperations.Utils;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Globalization;
using HBLib;
using RabbitMqEventBus.Events;
using Notifications.Base;
using HBMLHttpClient;
using Renci.SshNet.Common;
using UserOperations.Models.AnalyticModels;
using HBMLHttpClient.Model;
using System.Drawing;
using System.Transactions;
using FillingSatisfactionService.Helper;
using HBData.Repository;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController : Controller
    {
        private readonly IConfiguration _config;
        private readonly LoginService _loginService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly MailSender _mailSender;
        private readonly RequestFilters _requestFilters;
        private readonly SftpSettings _sftpSettings;
        private readonly DBOperations _dbOperation;
        private readonly IGenericRepository _repository;


        public HelpController(
            IConfiguration config,
            LoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            MailSender mailSender,
            RequestFilters requestFilters,
            SftpSettings sftpSettings,
            DBOperations dBOperations,
            IGenericRepository repository
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _mailSender = mailSender;
            _requestFilters = requestFilters;
            _sftpSettings = sftpSettings;
            _dbOperation = dBOperations;
            _repository = repository;
        }
        [HttpGet("Test")]
        public async Task<ActionResult> Test()
        => Ok();
    }
}