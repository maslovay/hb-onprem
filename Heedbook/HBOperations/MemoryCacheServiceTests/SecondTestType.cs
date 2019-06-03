using System;
using MemoryDbEventBus;

namespace MemoryCacheService.Tests
{
    public class SecondTestType : IMemoryDbEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; } = "Test2";
        public string Name { get; set; }
    }
}