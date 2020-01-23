using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace HBMLOnlineService.Service
{
    public class HBMLOnlineFaceService
    {
        private readonly HbMlHttpClient _client;
        private readonly RecordsContext _context;
        private readonly INotificationPublisher _publisher;
        private readonly SftpClient _sftpClient;
    

        public HBMLOnlineFaceService(RecordsContext context,
            HbMlHttpClient client,
            INotificationPublisher publisher, 
            SftpClient sftpClient
        )
        {
            _context = context;
            _client = client;
            _publisher = publisher;
            _sftpClient = sftpClient;
        }

        public async Task<List<HBMLHttpClient.Model.FaceResult>> UploadFrameAndGetFaceResultAsync(string base64String, string fileName, bool description, bool emotions, bool headpose, bool attributes)
        {
            
            System.Console.WriteLine("request");
            var faceResult = await _client.GetFaceResultWithParams(base64String, description, emotions, headpose, attributes);
            System.Console.WriteLine("Printing face result");
            System.Console.WriteLine(faceResult);

            if (faceResult.Any())
            {
                byte[] bytes = Convert.FromBase64String(base64String);
                var memoryStream = new MemoryStream(bytes);
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, "clientavatars/", fileName);
            }
            
            return faceResult;
        } 

        public void PublishMessageToRabbit(Guid? deviceId, Guid? companyId, string filename, List<HBMLHttpClient.Model.FaceResult> faceResults)
        {
            var personOnlineDetectionRun = new PersonOnlineDetectionRun
            {
                DeviceId = deviceId,
                Attributes = JsonConvert.SerializeObject(faceResults.First().Rectangle),
                Path = $"clientavatars/{filename}",
                CompanyId = companyId,
                Descriptor = JsonConvert.SerializeObject(faceResults.First().Descriptor),
                Age = Convert.ToInt32(faceResults.First().Attributes.Age),
                Gender = faceResults.First().Attributes.Gender,
            };
            _publisher.Publish(personOnlineDetectionRun);
        }   
    
    }

}