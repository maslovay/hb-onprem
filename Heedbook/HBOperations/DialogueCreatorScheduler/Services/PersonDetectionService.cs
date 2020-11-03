using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using HBData.Models;
using HBLib.Utils;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;

namespace DialogueCreatorScheduler.Services
{
    public class PersonDetectionService
    {

        private readonly DescriptorCalculations _calc;

        public PersonDetectionService(DescriptorCalculations calc)
        {
            _calc = calc;
        }

        public Guid? FindId(FileFrame fileFrame, List<Client> clients, double threshold=0.44)
        {
            if (!clients.Any()) return null;
            foreach(var client in clients.Where(p => p.FaceDescriptor != null).Distinct())
            {
                var cos = _calc.Cos(fileFrame.FrameAttribute.FirstOrDefault().Descriptor, JsonConvert.SerializeObject(client.FaceDescriptor));
                System.Console.WriteLine($"Cos distance is {cos} with client {client.ClientId}");
                if (cos > threshold) return client.ClientId;
            }
            return null;
        }
    }
}