using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class TabletLoadRun : IntegrationEvent
    {
        public String Command { get; set; }
        public Int32 NumberOfExtendedDevices { get; set; }
        public Int32 NumberOfNotExtendedDevices { get; set; }
        public Int32 WorkingTimeInMinutes{ get; set; }
    }
}
