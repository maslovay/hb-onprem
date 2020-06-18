using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonOnlineDetectionService.Utils;
using PersonOnlineDetectionService.Models;
using RabbitMqEventBus.Events;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PersonOnlineDetectionService
{
    public class PersonOnlineDetection
    {
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly PersonDetectionUtils _personDetectionUtils;
        private readonly CreateAvatarUtils _createAvatar;
        private readonly WebSocketIoUtils _socket;
        private readonly AkBarsOperations _akBarsOperations;

        public PersonOnlineDetection(
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            PersonDetectionUtils personDetectionUtils,
            CreateAvatarUtils createAvatar,
            WebSocketIoUtils socket,
            AkBarsOperations akBarsOperations
        )
        {
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _personDetectionUtils = personDetectionUtils;
            _socket = socket;
            _createAvatar = createAvatar;
            _akBarsOperations = akBarsOperations;
        }

        public async Task Run(PersonOnlineDetectionRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(message.Path);
            _log.Info("Function started");

            try
            {
                var begTime = DateTime.Now.AddDays(-30);
                var lastClientsInfo = _context.Clients
                    .Where(p => p.CompanyId == message.CompanyId 
                        && p.LastDate >= begTime 
                        && p.FaceDescriptor.Any()
                        && p.StatusId == 3)
                    .ToList();

                var clientId = await _akBarsOperations.FindClientIdInAkBarsApi(message.Path);
                if(clientId is null)
                    return;
                    
                var clientExistInHeedBookBase = _context.Clients
                    .Where(p => p.ClientId == clientId)
                    .Any();   
                if (!clientExistInHeedBookBase)
                {
                    var client = _personDetectionUtils.CreateNewClient(message, (Guid) clientId);
                    _log.Info($"Created client {JsonConvert.SerializeObject(client)}");
                    await _createAvatar.ExecuteAsync(message.Attributes, (Guid) clientId, message.Path);
                    _log.Info($"Created avatar with name {clientId}");
                    var result = _socket.Execute(room: message.DeviceId.ToString(), companyId: message.CompanyId.ToString(),
                        tabletId: message.DeviceId.ToString(), role: "tablet", clientId: clientId.ToString());
                    _log.Info("Send to webscoket");
                    _personDetectionUtils.CreateClientSession((Guid) clientId, $"{clientId}.jpg");
                    _log.Info("Created client session");
                }
                else
                {
                    _personDetectionUtils.CreateClientSession((Guid) clientId, message.Path);
                    var client = _context.Clients.FirstOrDefault(p => p.ClientId == clientId);
                    client.LastDate = DateTime.UtcNow;
                    System.Console.WriteLine($"clientlastTime: {client.LastDate}");
                    _context.SaveChanges();
                    
                    var result = _socket.Execute(room: message.DeviceId.ToString(), companyId: message.CompanyId.ToString(),
                        tabletId: message.DeviceId.ToString(), role: "tablet", clientId: clientId.ToString());

                    _log.Info("Send to webscoket");
                }
                _log.Info("Function finished");
            }
            catch (Exception e)
            {
                _log.Info($"Exception occured {e}");
                throw e;
            }
        }
    }
}
