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
        public async Task<ActionResult> Test([FromQuery]int skip, int take)
        {
            int counter = 0;
            var dialogues = _context.Dialogues.Include(x => x.DialogueClientProfile).Skip(skip).Take(take).ToList();
                    var activeStatusId = _context.Statuss
                                    .Where(x => x.StatusName == "Active")
                                    .Select(x => x.StatusId)
                                    .FirstOrDefault();
            foreach (var curDialogue in dialogues)
            {
                //   var curDialogue = _context.Dialogues.Include(x => x.DialogueClientProfile).FirstOrDefault(x => x.DialogueId.ToString() == id);
                try
                {
                    if (curDialogue.ClientId != null) continue;

                    var company = _context.ApplicationUsers
                                  .Where(x => x.Id == curDialogue.ApplicationUserId)
                                  .Select(x => x.Company)
                                  .FirstOrDefault();

                    Guid? personId = curDialogue.PersonId ?? Guid.NewGuid();

                    var dialogueClientProfile = curDialogue.DialogueClientProfile.FirstOrDefault();
                    if (dialogueClientProfile == null) continue;


                    double[] faceDescr = new double[0];
                    try
                    {
                        faceDescr = JsonConvert.DeserializeObject<double[]>(curDialogue.PersonFaceDescriptor);
                    }
                    catch { }
                    if (dialogueClientProfile.Age == null || dialogueClientProfile.Gender == null) continue;

                    Client client = new Client
                    {
                        ClientId = (Guid)personId,
                        CompanyId = (Guid)company?.CompanyId,
                        CorporationId = company?.CorporationId,
                        FaceDescriptor = faceDescr,
                        Age = (int)dialogueClientProfile?.Age,
                        Avatar = dialogueClientProfile?.Avatar,
                        Gender = dialogueClientProfile?.Gender,
                        StatusId = activeStatusId
                    };
                    _context.Clients.Add(client);
                    //  _context.SaveChanges();

                    curDialogue.ClientId = personId;
                    _context.SaveChanges();
                    counter++;
                }
                catch
                {
                    return null;
                }
            }
                    return Ok(counter);
        }
    }
}