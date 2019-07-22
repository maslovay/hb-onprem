using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class SlackClient
    {
        private readonly SlackSettings _settings;
	
        public SlackClient(SlackSettings settings)
        {
            _settings = settings;
        }
        public void PostMessage(Payload payload)
        {
            using(var client =  new HttpClient())
            {
                var data = new StringContent(JsonConvert.SerializeObject(payload));
                data.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                 client.PostAsync(_settings.Uri, data).GetAwaiter().GetResult();
            }
        }
    }
}