using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ClientAzureCheckingService
{
    public class ClientAzureChecking
    {
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;

        private readonly AzureClient _azureClient;

        public ClientAzureChecking(
            ElasticClientFactory elasticClientFactory,
            RecordsContext context,
            AzureClient azureClient
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));;
            _azureClient = azureClient ?? throw new ArgumentNullException(nameof(azureClient));
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Run(String path)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(path);

            try
            {
                _log.Info("Function started");
                var fileName = path.Split('/').Last();
                var client = _context.Clients.Where(p => p.Avatar == fileName).FirstOrDefault();
                if (client != null)
                {
                    var faceResult = await _azureClient.DetectGenderAgeAsync(path);
                    _log.Info($"Result of age gender detection - {JsonConvert.SerializeObject(faceResult)}");
                    if (faceResult != null)
                    {
                        client.Gender = faceResult.FirstOrDefault().FaceAttributes.Gender.ToString();
                        client.Age = (int) faceResult.FirstOrDefault().FaceAttributes.Age;
                    }
                    _context.SaveChanges();
                    _log.Info("Function finished");
                }
                else
                {
                    _log.Error($"No client with avatar filename {fileName}");
                    _log.Info("Function finished");
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                throw e;
            }

            
        }
    }
}