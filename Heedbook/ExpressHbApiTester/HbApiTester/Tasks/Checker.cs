using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HbApiTester.sqlite3;
using Renci.SshNet;

namespace HbApiTester.Tasks
{
    public class Checker
    {
        private readonly DbOperations _dbOperations;

        public Checker(DbOperations dbOperations)
        {
            _dbOperations = dbOperations;
        }

        public TestResponse Check<T>(T ttask)
            where T : TestTask
        {
            _dbOperations.InsertTask(ttask);

            if (ttask.Method == "ftp")
                return CheckFtp(ttask);
            
            var requestText = ttask.Url;

            if (ttask.Parameters != null && ttask.Parameters.Any())
                requestText += $"?{string.Join("", ttask.Parameters.Select(par => $"&{par.Key}={par.Value}"))}";
            
            try
            {
                // Create a request for the URL.   
                var request = WebRequest.Create(requestText);
                request.Method = ttask.Method;
                request.Timeout = 30000;
                if (request.Method == "POST")
                {
                    request.ContentLength = ttask.Body.Length;
                    request.ContentType = "application/json";
                }

                Console.WriteLine(requestText + ":" + ttask.Body);

                // If required by the server, set the credentials.  
                request.Credentials = CredentialCache.DefaultCredentials;
                if (!string.IsNullOrWhiteSpace(ttask.Token))
                {
                    request.Headers.Add("Accept", "text/plain");
                    request.Headers.Add("Authorization", ttask.Token);
                }

                if (request.Method == "POST")
                    using (var reqStream = request.GetRequestStream())
                    using (var sw = new StreamWriter(reqStream))
                        sw.Write(ttask.Body);

                // Get the response.  
                using (var response = request.GetResponse())
                {
                    // Display the status.  
                    Console.WriteLine(((HttpWebResponse) response).StatusDescription);

                    var status = ((HttpWebResponse) response).StatusCode;

                    var testResponse = new TestResponse()
                    {
                        TaskId = ttask.TaskId,
                        ResponseId = Guid.NewGuid(),
                        TaskName = ttask.Name,
                        IsPositive = status == HttpStatusCode.OK,
                        ResultMessage = status == HttpStatusCode.OK ? ttask.SuccessMessage : ttask.FailMessage,
                        Info = $"Status: {((HttpWebResponse) response).StatusCode.ToString()}",
                        Url = ttask.Url
                    };

                    switch (ttask)
                    {
                        case TestTaskWithDelayedResult _:
                            testResponse.IsFilled = false;
                            break;
                        case TestTask _:
                        {
                            using (var dataStream = response.GetResponseStream())
                            {
                                var reader = new StreamReader(dataStream);
                                testResponse.Body = reader.ReadToEnd();
                            }

                            testResponse.IsFilled = true;
                            testResponse.Timestamp = DateTime.Now;
                            break;
                        }
                    }

                    _dbOperations.InsertResponse(testResponse);
                    return testResponse;
                }
            }
            catch (WebException ex)
            {
                var failResponse = new TestResponse()
                {
                    TaskId = ttask.TaskId,
                    ResponseId = Guid.NewGuid(),
                    TaskName = ttask.Name,
                    IsPositive = false,
                    Info = $"Status: {ex.Status.ToString()} {ex.Message}",
                    Timestamp = DateTime.Now,
                    ResultMessage = ttask.FailMessage,
                    Url = ttask.Url,
                    IsFilled = true
                };

                return failResponse;
            }
        }

        private static TestResponse CheckFtp<T>(T ttask) where T : TestTask
        {
            var pattern = @"(.*):(.*)@(.*):(\d*)(.*)";
            var regex = new Regex(pattern);
            if (!regex.IsMatch(ttask.Url))
                throw new Exception($"TaskFactory.MakeCheckFtpAvailabilityTask() : incorrect connection string {ttask.Url}");

            var match = regex.Match(ttask.Url);
            var userName = match.Groups[1].Value;
            var userPassword = match.Groups[2].Value;
            var ip = match.Groups[3].Value;
            var port = int.Parse(match.Groups[4].Value);
            var directory = match.Groups[5].Value;

            var testResponse = new TestResponse()
            {
                TaskId = ttask.TaskId,
                ResponseId = Guid.NewGuid(),
                TaskName = ttask.Name,
                IsPositive = true,
                ResultMessage = string.Empty,
                Info = string.Empty,
                IsFilled = true,
                Timestamp = DateTime.Now,
                Url = ttask.Url
            };

            try
            {
                Console.WriteLine($"Connecting to ftp: {ip}:{port}");
                using (var client = new SftpClient(ip, port, userName, userPassword))
                {
                    client.Connect();
                    client.ListDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connecting to ftp: FAIL!");
                testResponse.Info = ex.Message;
                testResponse.IsPositive = false;
            }
            Console.WriteLine($"Connecting to ftp: OK!");

            return testResponse;
        }
    }
}