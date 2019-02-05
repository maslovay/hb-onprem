using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace AudioAnalyzeScheduler
{
    public class CheckAudioRecognizeStatusJob : IJob
    {
        private readonly IGenericRepository _repository;

        private readonly GoogleConnector _googleConnector;

        public CheckAudioRecognizeStatusJob(IServiceScopeFactory scopeFactory,
            GoogleConnector googleConnector)
        {
            var scope = scopeFactory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            _googleConnector = googleConnector;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var audios = await _repository.FindByConditionAsync<FileAudioDialogue>(item => item.StatusId == 1);

            var tasks = audios.Select(item =>
            {
               return Task.Run(async () =>
                {
                    var sttResults = await _googleConnector.GetGoogleSTTResults(item.TransactionId);
                    var differenceHour = (DateTime.Now - item.CreationTime).Hours;
                    if (sttResults.Words == null && differenceHour >= 1)
                    {
                        //8 - error
                        item.StatusId = 8;
                        _repository.Update(item);
                        _repository.Save();
                    }
                });
            }).ToList();

            await Task.WhenAll(tasks);
        }
    }
}