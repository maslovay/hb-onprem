using System;

namespace MemoryDbEventBus
{
    public interface IMemoryDbEvent
    {
        Guid Id { get; set; }
    }
}