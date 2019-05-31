using System;
using MemoryDbEventBus;

namespace MemoryCacheService.Tests
{
    public class FirstTestType : IMemoryDbEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; } = "Test1";
        public int Status { get; set; }
    }
}