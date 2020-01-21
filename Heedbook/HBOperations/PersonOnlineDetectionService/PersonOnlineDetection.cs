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
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly SftpClient _sftpclient;
        private readonly PersonDetectionUtils _personDetectionUtils;
        private readonly WebSocketIoUtils _socket;

        public PersonOnlineDetection(
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            SftpClient sftpclient,
            PersonDetectionUtils personDetectionUtils,
            WebSocketIoUtils socket
        )
        {
            // _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _sftpclient = sftpclient;
            _personDetectionUtils = personDetectionUtils;
            _socket = socket;
        }

        public async Task Run(PersonOnlineDetectionRun message)
        {
            System.Console.WriteLine("Function started");
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(message.Path);
            _log.Info("Function started");

            System.Console.WriteLine(JsonConvert.SerializeObject(message));

            try
            {
                var begTime = DateTime.Now.AddDays(-30);
                var lastClientsInfo = _context.ClientNotes
                    .Include(p => p.Client)
                    .Where(p => p.Client.CompanyId == message.CompanyId && p.CreationDate >= begTime)
                    .ToList();
                
                var clientId = _personDetectionUtils.FindId(message.Descriptor, lastClientsInfo);
                if (clientId == null)
                {
                    clientId = Guid.NewGuid();
                    var client = _personDetectionUtils.CreateNewClient(message, (Guid) clientId);
                    _sftpclient.RenameFile(message.Path, $"useravatars/{client.ClientId}.jpg");
                    var result = _socket.Execute(room: message.DeviceId.ToString(), companyId: message.CompanyId.ToString(),
                        tabletId: message.DeviceId.ToString(), role: "tablet", clientId: clientId.ToString());
                    System.Console.WriteLine(result);
                }
                else
                {
                    await _sftpclient.DeleteFileIfExistsAsync(message.Path);
                    var result = _socket.Execute(room: message.DeviceId.ToString(), companyId: message.CompanyId.ToString(),
                        tabletId: message.DeviceId.ToString(), role: "tablet", clientId: clientId.ToString());
                    System.Console.WriteLine(result);
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