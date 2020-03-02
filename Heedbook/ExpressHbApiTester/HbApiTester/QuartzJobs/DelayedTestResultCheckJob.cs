using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System.Threading.Tasks;
using HbApiTester.Settings;
using HbApiTester.sqlite3;
using HbApiTester.Tasks;

namespace HbApiTester.QuartzJobs
{
    public class DelayedTestResultCheckJob : IJob
    {
        private readonly DbOperations _dbOperations;
        private readonly DelayedTestsRunner _delayedTestsRunner;
        
        public DelayedTestResultCheckJob(DbOperations dbOperations, DelayedTestsRunner delayedTestsRunner)
        {
            _dbOperations = dbOperations;
            _delayedTestsRunner = delayedTestsRunner;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("DelayedTestResultCheckJob.Execute...");
            var delayedTasks =
                _dbOperations.GetTasks<TestTaskWithDelayedResult>(t =>  (DateTime.Now - t.StartedAt).TotalMinutes > t.DelayInMinutes);

            foreach (var dt in delayedTasks)
            {
                // check for response existance
                var response = _dbOperations.GetResponse(r => r.TaskId == dt.TaskId && r.IsFilled);
                if (response == null)
                {
                    response = _dbOperations.GetResponse(r => r.TaskId == dt.TaskId) ?? new TestResponse()
                    {
                        ResponseId = Guid.NewGuid(),
                        TaskId =  dt.TaskId,
                    };

                    response.IsPositive = false;
                    response.ResultMessage =
                        $"No filled response for task {dt.Name} : {dt.TaskId} after {dt.DelayInMinutes} min of running!";
                    
                    _dbOperations.DeleteTask<TestTaskWithDelayedResult>( t => t.TaskId == dt.TaskId );
                    _delayedTestsRunner.InvokeError(response);
                    continue;
                }

                // check if response positive
                if (!response.IsPositive)
                    _delayedTestsRunner.InvokeError(response);
                else
                    _delayedTestsRunner.InvokeSuccess(response);
                
                _dbOperations.DeleteTask<TestTaskWithDelayedResult>( t => t.TaskId == dt.TaskId );
            }
        }
    }
}