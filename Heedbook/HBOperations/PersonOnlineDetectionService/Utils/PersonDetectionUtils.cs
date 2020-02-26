using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using HBData.Models;
using HBLib.Utils;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;

namespace PersonOnlineDetectionService.Utils
{
    public class PersonDetectionUtils
    {

        private readonly DescriptorCalculations _calc;
        private readonly RecordsContext _context;

        public PersonDetectionUtils(DescriptorCalculations calc,
            RecordsContext context)
        {
            _calc = calc;
            _context = context;
        }

        public Guid? FindId(string descriptor, List<Client> clients, double threshold=0.4)
        {
            if (!clients.Any()) return null;
            foreach(var client in clients.Distinct())
            {
                var cos = _calc.Cos(descriptor, JsonConvert.SerializeObject(client.FaceDescriptor));
                System.Console.WriteLine($"Cos distance is {cos} with client {client.ClientId}");
                if (cos > threshold) return client.ClientId;
            }
            return null;
        }

        public Client CreateNewClient(PersonOnlineDetectionRun message, Guid clientId)
        {
            var corporationId = _context.Companys.Where(p => p.CompanyId == message.CompanyId).First().CorporationId;
            Client client = new Client
            {
                ClientId = clientId,
                CompanyId = (Guid) message.CompanyId,
                CorporationId = corporationId,
                FaceDescriptor = JsonConvert.DeserializeObject<double[]>(message.Descriptor),
                Age = message.Age,
                Avatar = $"{clientId}.jpg",
                Gender = message.Gender,
                StatusId = 3,
                LastDate = DateTime.UtcNow
            };
            _context.Clients.Add(client);
            _context.SaveChanges();
            return client;
        }


    }
}