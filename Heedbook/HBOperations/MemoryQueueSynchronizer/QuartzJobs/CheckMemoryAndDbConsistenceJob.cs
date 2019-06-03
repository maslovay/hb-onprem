using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using RabbitMqEventBus;
using System;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using MemoryDbEventBus;
using MemoryDbEventBus.Events;


namespace MemoryQueueSynchronizer.QuartzJobs
{
    public class CheckMemoryAndDbConsistenceJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly IMemoryDbPublisher _memoryDbPublisher;
        private readonly IMemoryCache _memoryCache;

        public CheckMemoryAndDbConsistenceJob(IServiceScopeFactory factory,
            ElasticClient log,
            IMemoryDbPublisher memoryPublisher,
            IMemoryCache memoryCache)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _log = log;
            _memoryDbPublisher = memoryPublisher;
            _memoryCache = memoryCache;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _log.Info("Checking DB and MemoryQueue (Redis and etc...) consistence!");
            try
            {
                var stillUnprocessedDialogs = _context.Dialogues.Where(d => d.StatusId == 6);
                
                foreach ( var dialog in stillUnprocessedDialogs )
                    if (!_memoryCache.HasRecords<DialogueCreatedEvent>(x => x.Id == dialog.DialogueId))
                    {
                        if (dialog.StatusId == null) 
                            continue;
                        
                        var dialogueCreatedEvent = new DialogueCreatedEvent()
                        {
                            Id = dialog.DialogueId,
                            Status = dialog.StatusId.Value
                        };
                        _memoryDbPublisher.Publish(dialogueCreatedEvent);
                    }
                
                
                var stillUnprocessedFileAudioDialogs = _context.FileAudioDialogues.Where(fad => fad.StatusId == 6);
                
                foreach ( var fad in stillUnprocessedFileAudioDialogs )
                    if (!_memoryCache.HasRecords<FileAudioDialogueCreatedEvent>(x => x.Id == fad.DialogueId))
                    {
                        if (fad.StatusId == null) 
                            continue;
                        
                        var fileAudioDialogueCreatedEvent = new FileAudioDialogueCreatedEvent()
                        {
                            Id = fad.DialogueId,
                            Status = fad.StatusId.Value
                        };
                        
                        _memoryDbPublisher.Publish(fileAudioDialogueCreatedEvent);
                    }
                
                _log.Info("Function CheckMemoryAndDbConsistence finished");                
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception while executing CheckMemoryAndDbConsistence occured {e}");
                throw;
            }
        }
    }
}