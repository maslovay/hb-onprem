using System;
using System.Diagnostics;
using RabbitMqEventBus.Events;
using RabbitMqEventBus;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace UnitAPITestsService.Tasks
{
    public class Checker
    {
        private Process _shedulerProcess;
        private readonly INotificationPublisher _publisher;
        public Checker(INotificationPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Check()
        {
            try
            {   
                System.Console.WriteLine($"runned");
                var dockerEnvironment = Environment.GetEnvironmentVariable("DOCKER_UNIT_TEST_ENVIRONMENT")=="TRUE" ? true : false;
                var info = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    //Arguments = "vstest ../ApiTests/bin/Debug/netcoreapp2.2/ApiTests.dll",
                    //Arguments = "vstest ApiTests.dll",
                    Arguments = dockerEnvironment ? "vstest ApiTests.dll" : "vstest ../ApiTests/bin/Debug/netcoreapp2.2/ApiTests.dll",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                _shedulerProcess = Process.Start(info);
                var testResults = _shedulerProcess.StandardOutput.ReadToEnd();
                _shedulerProcess.WaitForExit();
                var resultArray = testResults.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);                
                
                var report = PrepareReport(resultArray);

                System.Console.WriteLine(report);             
                var message = new MessengerMessageRun
                {
                    logText = report,
                    ChannelName = "IntegrationTester"
                };
                System.Console.WriteLine($"{JsonConvert.SerializeObject(message)}");
                _publisher.Publish(message);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }
        private string PrepareReport(string[] resultArray)
        {
            var regex = new Regex(@"at (\w|\W)* in");
            var report = "\n";
                
            var index = Array.FindIndex(resultArray, s => s.Contains($"Failed:"));
            if(index >= 0)
            {
                report += $"Tests report:\n";
                report += $"{resultArray[index-2]}\n";
                report += $"{resultArray[index-1]}\n";
                report += $"{resultArray[index]}\n";
                report += $"{resultArray[index+1]}\n\n";

                report += $"Failed tests:\n";
                for(int i = 0; i < resultArray.Length; i++)
                {
                    if(regex.IsMatch(resultArray[i]))
                    {
                        var matchString = regex.Matches(resultArray[i])[0].Value;                            
                        report += $"X {matchString.Split(" ")[1]}\n";
                    }
                }
            }
            else
            {
                index = Array.FindIndex(resultArray, s => s.Contains($"Test Run Successful."));
                report += $"{resultArray[index]}\n";
                report += $"{resultArray[index+1]}\n";
                report += $"{resultArray[index+2]}\n";
                report += $"{resultArray[index+3]}\n\n";
            }   

            return report;
        }
    }
}