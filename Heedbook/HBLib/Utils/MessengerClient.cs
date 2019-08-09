using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class MessengerClient
    {
        private readonly MessengerSettings _settings;
	
        public MessengerClient(MessengerSettings settings)
        {
            _settings = settings;
        }
        public void PostMessage(Message message)
        {
            using(var client =  new HttpClient())
            {
                var data = new StringContent(JsonConvert.SerializeObject(message));
                data.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                 client.PostAsync(_settings.Uri, data).GetAwaiter().GetResult();
            }
        }
    }
}