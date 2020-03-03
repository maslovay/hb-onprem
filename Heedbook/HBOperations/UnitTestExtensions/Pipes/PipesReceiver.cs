using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Newtonsoft.Json;
using RabbitMqEventBus.Base;

namespace UnitTestExtensions
{
    public class PipesReceiver<T> : IDisposable
    where T : IntegrationEvent
    {
        private bool isWorking = false;
        private readonly NamedPipeClientStream _clientPipeStream;
        private IIntegrationEventHandler<T> _handler;
        private readonly Thread workerThread;
        
        public PipesReceiver(IIntegrationEventHandler<T> handler)
        {
            _clientPipeStream = new NamedPipeClientStream(".",typeof(T) + ".pipe", PipeDirection.In);
            _handler = handler;
            workerThread = new Thread(Worker);
            workerThread.Start();
        }


        private void Worker()
        {
            _clientPipeStream.Connect();

            using (var sr = new StreamReader(_clientPipeStream))
            {
                while (isWorking)
                {
                    while (!sr.EndOfStream)
                    {
                        var text = sr.ReadToEnd();

                        try
                        {
                            IntegrationEvent integrationEvent = JsonConvert.DeserializeObject<IntegrationEvent>(text);
                            
                            _handler.Handle((T)integrationEvent);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    
                    Thread.Sleep(100);
                }
            }
        }
        
        public void Dispose()
        {
            isWorking = false;
            workerThread.Join(10000);
            _clientPipeStream.Close();
        }
    }
}