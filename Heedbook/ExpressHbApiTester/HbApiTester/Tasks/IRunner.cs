namespace HbApiTester.Tasks
 {
     public interface IRunner
     {
         event TestsRunner.ApiEvent ApiError;
         event TestsRunner.ApiEvent ApiSuccess;
         void RunTests(bool needAuth = true);
         void DoTest(TestTask task, int tries = 3, int delay = 2000);
     }
 }