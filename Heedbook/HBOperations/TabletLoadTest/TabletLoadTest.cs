using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions.Utils.WeeklyReport;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;
using UserOperations.Models;
using UserOperations.Services.Interfaces;

namespace TabletLoadTest
{
    public class TabletLoad
    {
        private readonly ElasticClientFactory _elasticClientFactory;
        private List<Task> tabletTasks;
        private IGenericRepository _repository;
        private Object lockObject = new Object();
        ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
        private List<IScheduler> schedulerList = new List<IScheduler>();
        private List<TestAccount> testAccountsList = new List<TestAccount>();
        private INotificationPublisher _publisher;
        private IServiceProvider _serviceProvider;
        public TabletLoad(
            ElasticClientFactory elasticClientFactory,
            IGenericRepository repository,
            INotificationPublisher publisher,
            IServiceProvider serviceProvider)
        {
            _elasticClientFactory = elasticClientFactory;
            _repository = repository;
            _publisher = publisher;
            _serviceProvider = serviceProvider;
        }

        public void Run(TabletLoadRun message)
        {
            var startTime = DateTime.Now;
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(message.Command);
            tabletTasks = new List<Task>();
            var testAccountsList = new List<TestAccount>();
            try
            {
                if (message.Command == "start")
                {
                    System.Console.WriteLine($"started process");
                    for (int i = 0; i < message.NumberOfNotExtendedDevices; i++)
                    {
                        CreateTabletSchedulers(false).Wait();
                    }
                    for (int i = 0; i < message.NumberOfExtendedDevices; i++)
                    {
                        CreateTabletSchedulers(true).Wait();
                    }
                    Task.WhenAll(tabletTasks);
                    System.Console.WriteLine($"AllTabletCreated");
                    while((DateTime.Now - startTime).Minutes < message.WorkingTimeInMinutes)
                    {
                        System.Console.WriteLine(".");
                        Thread.Sleep(1000);
                    }
                    ClearAllTestAccountsFromDB();
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                System.Console.WriteLine(e);
                ClearAllTestAccountsFromDB();
            }
        }
        private void ClearAllTestAccountsFromDB()
        {
            foreach (var sch in schedulerList)
                sch.Shutdown();
            foreach (var testAcc in testAccountsList)
                testAcc.DeleteTestAccountData().Wait();
            System.Console.WriteLine($"cleared all TestAccount");
        }
        public async Task CreateTabletSchedulers(Boolean IsExtended)
        {
            var _testAccount = new TestAccount(_repository, IsExtended);
            _testAccount.PrepareTestAccount().Wait();
            testAccountsList.Add(_testAccount);

            var dictionary = new List<Tuple<int, bool, string>>
            {
                new Tuple<int, bool, string>(180, true, "GenerateTokenRequest"),
                new Tuple<int, bool, string>(180, true, "DeviceGenerateTokenRequest"),
                //commented due to token generation
                // new Tuple<int, bool, string>(180, true, "AccountChangePasswordRequest"),
                new Tuple<int, bool, string>(180, true, "CampaignContentRequest"),
                new Tuple<int, bool, string>(180, true, "DeviceEmployeeRequest"),
                new Tuple<int, bool, string>(180, true, "CampaignContentCampaignRequest"),
                new Tuple<int, bool, string>(180, true, "DemonstrationV2FlushStatsRequest"),
                new Tuple<int, bool, string>(180, true, "DemonstrationV2PoolAnswerRequest"),
                new Tuple<int, bool, string>(180, true, "SessionAlertNotSmileRequest"),
                new Tuple<int, bool, string>(180, false, "FillingFileFrameRequest"),
                new Tuple<int, bool, string>(180, false, "FaceRequest"),
                new Tuple<int, bool, string>(180, true, "LogSaveRequest"),
                new Tuple<int, bool, string>(180, true, "VideoSaveInfoRequest")
            };
            foreach (var pair in dictionary)
            {
                if (_testAccount._company.IsExtended && pair.Item2)
                    CreateSchedulerAndPutToList(pair.Item3, pair.Item1, _testAccount).Wait();
                else if (!_testAccount._company.IsExtended)
                    CreateSchedulerAndPutToList(pair.Item3, pair.Item1, _testAccount).Wait();
            }
        }
        private async Task CreateSchedulerAndPutToList(string commandName, int seconds, TestAccount testAccount)
        {
            var schedulerModel = new SchedulerModel
            {
                RequestName = commandName,
                DeviceId = testAccount._device.DeviceId,
                CompanyId = testAccount._company.CompanyId,
                ApplicationUserId = testAccount._applicationUser.Id
            };
            
            var scheduler = await schedulerFactory.GetScheduler();
            scheduler.JobFactory = new RequestJobFactory(_serviceProvider);

            await scheduler.Start();            

            var job = JobBuilder.Create<RequestJob>()
                .UsingJobData("SchedulerModel", JsonConvert.SerializeObject(schedulerModel))
                .WithIdentity($"{commandName}Job", $"{testAccount._company.CompanyId}")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{commandName}Trigger", $"{testAccount._company.CompanyId}")
                .StartNow()
                .WithSimpleSchedule(p =>
                    p.WithIntervalInSeconds(seconds)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            schedulerList.Add(scheduler);
        }
    }
}