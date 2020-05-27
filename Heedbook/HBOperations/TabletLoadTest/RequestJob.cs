using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace TabletLoadTest
{
    public class RequestJob : IJob
    {
        private INotificationPublisher _publisher;
        public RequestJob(INotificationPublisher publisher)
        {
            _publisher = publisher;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerModel = JsonConvert.DeserializeObject<SchedulerModel>(context.JobDetail.JobDataMap.GetString("SchedulerModel"));
            var model = new TabletRequestRun
            {
                RequestName = schedulerModel.RequestName,
                DeviceId = schedulerModel.DeviceId,
                CompanyId = schedulerModel.CompanyId,
                ApplicationUserId = schedulerModel.ApplicationUserId
            };
            System.Console.WriteLine($"{model.RequestName} sended");
            _publisher.Publish(model);
        }
    }
}