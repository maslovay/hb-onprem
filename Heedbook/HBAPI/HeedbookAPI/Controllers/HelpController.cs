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
        public async Task<ActionResult> Test([FromQuery]string id)
        {
            var curDialogue = _context.Dialogues.Include(x => x.DialogueClientProfile).FirstOrDefault(x => x.DialogueId.ToString() == id);
                try
                {
                    var company = _context.ApplicationUsers
                                  .Where(x => x.Id == curDialogue.ApplicationUserId)
                                  .Select(x => x.Company)
                                  .FirstOrDefault();

                    Guid? clientId = _context.Clients
                            .Where(x => x.ClientId == curDialogue.PersonId)
                            .Select(x => x.ClientId).FirstOrDefault();

                
                if (clientId != null && clientId != Guid.Empty) return BadRequest();
                if (curDialogue.PersonId == null) clientId = Guid.NewGuid();
                else
                    clientId = curDialogue.PersonId;

                var d = _context.DialogueClientProfiles
                                    .Where(x => x.DialogueId == curDialogue.DialogueId).ToList();


                var dialogueClientProfile = _context.DialogueClientProfiles
                                    .FirstOrDefault(x => x.DialogueId == curDialogue.DialogueId);
                    if (dialogueClientProfile == null) return null;

                    var activeStatusId = _context.Statuss
                                    .Where(x => x.StatusName == "Active")
                                    .Select(x => x.StatusId)
                                    .FirstOrDefault();

                    double[] faceDescr = new double[0];
                    try
                    {
                        faceDescr = JsonConvert.DeserializeObject<double[]>(curDialogue.PersonFaceDescriptor);
                    }
                    catch { }
                    Client client = new Client
                    {
                        ClientId = (Guid)clientId,
                        CompanyId = (Guid)company?.CompanyId,
                        CorporationId = company?.CorporationId,
                        FaceDescriptor = faceDescr,
                        Age = (int)dialogueClientProfile?.Age,
                        Avatar = dialogueClientProfile?.Avatar,
                        Gender = dialogueClientProfile?.Gender,
                        StatusId = activeStatusId
                    };
                    _context.Clients.Add(client);
                    _context.SaveChanges();

                    curDialogue.ClientId = clientId;
                    _context.SaveChanges();
                    return Ok(client.ClientId);
                }
                catch
                {
                    return null;
                }
        }
    }
}