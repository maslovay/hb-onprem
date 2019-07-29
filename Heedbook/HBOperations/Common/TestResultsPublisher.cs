using System;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Common
{
    public class TestResultsPublisher
    {
        public delegate void TestFixtureStartedDelegate(string testFixtureName, string message);
        public delegate void TestResultReceivedDelegate(string testName, bool isPassed, string message, string errorMessage);
        public delegate void TestFixtureFinishedDelegate(string testFixtureName, string message);

        public event TestFixtureStartedDelegate TestFixtureStarted;
        public event TestResultReceivedDelegate TestResultReceived;
        public event TestFixtureFinishedDelegate TestFixtureFinished;
        
        protected void PublisherSetup()
        {
            var dateTime = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            var testFixtureName = TestContext.CurrentContext.Test.ClassName;
            TestFixtureStarted?.Invoke($"{dateTime} Test fixture:  {testFixtureName}", "Started");
        }

        protected void PublisherTearDown()
        {
            var dateTime = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            var testFixtureName = TestContext.CurrentContext.Test.ClassName;
            TestFixtureFinished?.Invoke($"{dateTime} Test fixture:  {testFixtureName}", "Finished");
        }

        [OneTimeSetUp]
        protected void PublisherEachTestSetup()
        {
            var dateTime = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            var testName = TestContext.CurrentContext.Test.FullName;
            TestFixtureStarted?.Invoke($"{dateTime} Test:  {testName}", "Started");            
        }

        [OneTimeTearDown]
        protected void PublisherEachTestTearDown()
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var testName = TestContext.CurrentContext.Test.FullName;
            var message = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            
            switch (status)
            {
                case TestStatus.Failed:
                    message += $" {testName} result: Failed";
                    break;
                case TestStatus.Passed:
                    message += $" {testName} result: Passed";
                    break;
                case TestStatus.Skipped:
                    message += $" {testName} result: Skipped";
                    break;                   
                case TestStatus.Warning:
                    message += $" {testName} result: Warning";
                    break;   
                default:
                case TestStatus.Inconclusive:
                    message += $" {testName} result: Inconclusive";
                    break;      
            }
            
            TestResultReceived?.Invoke(testName, status == TestStatus.Passed, message, 
                TestContext.CurrentContext.Result.Message);  
        }
    }
}