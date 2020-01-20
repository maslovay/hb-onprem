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
    

        public HBMLOnlineFaceService(RecordsContext context, INotificationPublisher publisher, SftpClient sftpClient)
        {
            _context = context;
            _publisher = publisher;
            _sftpClient = sftpClient;
        }

        public async Task<List<HBMLHttpClient.Model.FaceResult>> UploadFrameAndGetFaceResultAsync(byte[] file, string fileName, bool description, bool emotions, bool headpose, bool attributes)
        {
            var memoryStream = new MemoryStream(file);
            
            var base64String = Convert.ToBase64String(file);
            var faceResult = await _client.GetFaceResultWithParams(base64String, description, emotions, headpose, attributes);

            if (faceResult.Any())
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, "useravatars/", fileName);
            
            return faceResult;
        } 

        public void PublishMessageToRabbit(Guid? deviceId, Guid? companyId, string filename, List<HBMLHttpClient.Model.FaceResult> faceResults)
        {
            var personOnlineDetectionRun = new PersonOnlineDetectionRun
            {
                DeviceId = deviceId,
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