using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Newtonsoft.Json;
using RabbitMqEventBus.Base;

namespace UnitTestExtensions
{
    public class PipesSender : IDisposable
    {
        private readonly Dictionary<string, NamedPipeServerStream> _pipeServerStreams = 
            new Dictionary<string, NamedPipeServerStream>(10);

        public void RegisterPipe<T>()
            where T : IntegrationEvent
        {
            var type = typeof(T);
            var typeName = type.Name;

            if (!_pipeServerStreams.ContainsKey(typeName))
                _pipeServerStreams[typeName] = new NamedPipeServerStream(typeName + ".pipe", PipeDirection.InOut);
        }

        public void SendEventMessage(IntegrationEvent @eventMsg)
        {
            var text = JsonConvert.SerializeObject(eventMsg);
            var type = typeof(IntegrationEvent);
            var typeName = type.Name;
            var pipe = _pipeServerStreams[typeName];

            pipe?.Write(Encoding.UTF8.GetBytes(text));
        }
        
        public void Dispose()
        {
            foreach ( var val in _pipeServerStreams.Values )
               val.Close();
        }
    }
}