using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System.Threading.Tasks;
using HbApiTester.Settings;
using HbApiTester.Tasks;

namespace HbApiTester.QuartzJobs
{
    public class HbApiTesterJob : IJob
    {
        private readonly ApiTestsRunner _apiTestsRunner;
        private readonly DelayedTestsRunner _delayedTestsRunner;
        private readonly ExternalResourceTestsRunner _externalResourceTestsRunner;

        public HbApiTesterJob(ApiTestsRunner apiTestsRunner, DelayedTestsRunner delayedTestsRunner, ExternalResourceTestsRunner externalResourceTestsRunner)
        {
            _apiTestsRunner = apiTestsRunner;
            _delayedTestsRunner = delayedTestsRunner;
            _externalResourceTestsRunner = externalResourceTestsRunner;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _apiTestsRunner.RunTests();
            _delayedTestsRunner.RunTests();
            _externalResourceTestsRunner.RunTests(false);
        }
    }
}