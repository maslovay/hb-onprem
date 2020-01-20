using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;
using HBLib;
using HBData;
using PersonDetectionService.Exceptions;

namespace PersonDetectionService
{
    public class PersonDetection
    {
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly DescriptorCalculations _calc;


        public PersonDetection(
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            DescriptorCalculations calc
        )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _calc = calc;
        }

        public async Task Run(PersonDetectionRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{deviceIds}");
            _log.SetArgs(JsonConvert.SerializeObject(message.DeviceIds));
            _log.Info("Function started");
            try
            {
                var begTime = DateTime.Now.AddYears(-1);
                var companyIds = _context.Devices.Where(x => message.DeviceIds.Contains(x.DeviceId)).Select(x => x.CompanyId).Distinct().ToList();              

                //---dialogues for users in company or for devices in company
                var dialogues = _context.Dialogues
                    .Where(p => ( companyIds.Contains(p.Device.CompanyId)) && !String.IsNullOrEmpty(p.PersonFaceDescriptor) && p.BegTime >= begTime)
                    .OrderBy(p => p.BegTime)
                    .ToList();
                
                foreach (var curDialogue in dialogues.Where(p => p.PersonId == null).ToList())
                {
                    var dialoguesProceeded = dialogues
                        .Where(p => p.PersonId != null &&
                         ((p.ApplicationUserId != null && p.ApplicationUserId == curDialogue.ApplicationUserId)
                            || p.DeviceId != null && p.DeviceId == curDialogue.DeviceId))
                        .ToList();
                    curDialogue.PersonId = FindId(curDialogue, dialoguesProceeded);
                    try
                    {
                        string error = String.Empty;
                        (curDialogue.ClientId, error)  = CreateNewClient(curDialogue);//clientId = personId (the same)
                        if (error != String.Empty)
                            _log.Error($"client { curDialogue.PersonId  } creation error: {error}");
                        else
                            _log.Info($"client { curDialogue.PersonId  } created");
                    }
                    catch( Exception ex )
                    {
                        _log.Error($"client {curDialogue.PersonId} creation error: " + ex.Message);
                    }
                }
                _context.SaveChanges();
                _log.Info("Function finished");
            }
            catch (Exception e)
            {
                _log.Info($"Exception occured {e}");
                throw new PersonDetectionException(e.Message, e);
            }
        }

        public Guid? FindId(Dialogue curDialogue, List<Dialogue> dialogues, double threshold=0.42)
        {
            if (!dialogues.Any()) return Guid.NewGuid();
            foreach (var dialogue in dialogues)
            {
                var cosResult = _calc.Cos(curDialogue.PersonFaceDescriptor, dialogue.PersonFaceDescriptor);
                System.Console.WriteLine($"Cos distance is -- {cosResult}");
                if (cosResult > threshold) return dialogue.PersonId;
            }
            return Guid.NewGuid();
        }

        public (Guid?, string) CreateNewClient(Dialogue curDialogue)
        {
            Company company = null;
            if(curDialogue.ApplicationUserId != null)
            company = _context.ApplicationUsers
                              .Where(x => x.Id == curDialogue.ApplicationUserId).Select(x => x.Company).FirstOrDefault();
            else
            company = _context.Devices
                              .Where(x => x.DeviceId == curDialogue.DeviceId).Select(x => x.Company).FirstOrDefault();

            var clientId = _context.Clients
                        .Where(x => x.ClientId == curDialogue.PersonId)
                        .Select(x => x.ClientId).FirstOrDefault();
                if (clientId != null && clientId != Guid.Empty) return (clientId, String.Empty);

                var dialogueClientProfile = _context.DialogueClientProfiles
                                .FirstOrDefault(x => x.DialogueId == curDialogue.DialogueId);
                if (dialogueClientProfile == null) return (null, "client exception -  dialogueClientProfile == null");
                if (dialogueClientProfile.Age == null || dialogueClientProfile.Gender == null) return (null, "client exception -  dialogueClientProfile == null");

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
                    ClientId = (Guid)curDialogue.PersonId,
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
                return (client.ClientId, String.Empty);
        }
    }
}