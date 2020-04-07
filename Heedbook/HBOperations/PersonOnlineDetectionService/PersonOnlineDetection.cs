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
using RabbitMqEventBus;

namespace PersonOnlineDetectionService
{
    public class PersonOnlineDetection
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly PersonDetectionUtils _personDetectionUtils;
        private readonly CreateAvatarUtils _createAvatar;
        private readonly WebSocketIoUtils _socket;
        private readonly INotificationPublisher _publisher; 


        public PersonOnlineDetection(
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            PersonDetectionUtils personDetectionUtils,
            CreateAvatarUtils createAvatar,
            WebSocketIoUtils socket,
            INotificationPublisher publisher
        )
        {
            // _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _personDetectionUtils = personDetectionUtils;
            _socket = socket;
            _createAvatar = createAvatar;
            _publisher = publisher;
        }

        public async Task Run(PersonOnlineDetectionRun message)
        {
            System.Console.WriteLine(message.Path);
            System.Console.WriteLine(message.DeviceId);
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
                // var lastClientsInfo = _context.ClientNotes
                    // .Include(p => p.Client)
                    // .Where(p => p.Client.CompanyId == message.CompanyId && p.CreationDate >= begTime)
                    // .ToList();

                
                var clientId = _personDetectionUtils.FindId(message.Descriptor, lastClientsInfo);
                if (clientId == null)
                {
                    clientId = Guid.NewGuid();
                    _log.Info($"New client -- {clientId}");
                    var client = _personDetectionUtils.CreateNewClient(message, (Guid) clientId);
                    _log.Info($"Created client {JsonConvert.SerializeObject(client)}");
                    await _createAvatar.ExecuteAsync(message.Attributes, (Guid) clientId, message.Path);
                    _log.Info($"Created avatar with name {clientId}");
                    var result = _socket.Execute(room: message.DeviceId.ToString(), companyId: message.CompanyId.ToString(),
                        tabletId: message.DeviceId.ToString(), role: "tablet", clientId: clientId.ToString());
                    _log.Info("Send to webscoket");
                    _personDetectionUtils.CreateClientSession((Guid) clientId, $"{clientId}.jpg");
                    _log.Info("Created client session");
                    // System.Console.WriteLine(result);
                    var clientAzureMessage = new ClientAzureCheckingRun{
                        Path = $"clientavatars/{clientId}.jpg"
                    };
                    _publisher.Publish(clientAzureMessage);
                    
                }
                else
                {
                    var curTime = DateTime.UtcNow;
                    _personDetectionUtils.CreateClientSession((Guid) clientId, message.Path);
                    lastClientsInfo.Where(p => p.ClientId == clientId).ToList().ForEach(p => p.LastDate = curTime);
                    _context.SaveChanges();
                    // System.Console.WriteLine("Last time updated");
                    // await _createAvatar.DeleteFileAsync(message.Path);
                    var result = _socket.Execute(room: message.DeviceId.ToString(), companyId: message.CompanyId.ToString(),
                        tabletId: message.DeviceId.ToString(), role: "tablet", clientId: clientId.ToString());
                    _log.Info("Send to webscoket");


                    // System.Console.WriteLine(result);
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
