using NUnit.Framework.Constraints;

namespace DialogueStatusCheckerScheduler.Tests
{
    public class StubService
    {
        public bool Flag { get; private set; } = false;
        
        public void SetFlag()
        {
            Flag = true;
        }
    }
}