﻿using System;
using System.Collections.Generic;
using System.Text;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class ToneAnalyzeRun: IntegrationEvent
    {
        public String Path { get; set; }
    }
}
