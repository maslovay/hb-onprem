using System;
using System.Linq;
using System.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MemoryDbEventBus.Utils
{
    public static class TypeChecker
    {
        public static bool CorrespondsTo(JObject source, Type typeForCheck)
        {
            if (source.ContainsKey("EventType"))
            {
                JToken eventType; 
                source.TryGetValue("EventType", StringComparison.InvariantCulture, out eventType);
                return eventType.Value<string>() == typeForCheck.Name;
            }
            else
                return false;
        }

        public static dynamic MatchAndConvert(JObject source, string[] typeNames)
        {
            if ( source == null || typeNames == null )
                throw new InvalidOperationException("Source or typeNames == null!");
            
            foreach (string name in typeNames)
            {
                var type = GetTypeByName(name);

                if (CorrespondsTo(source, type))
                    return JsonConvert.DeserializeObject(source.ToString(), type);
            }

            return JsonConvert.DeserializeObject<MemoryDbEvent>(source.ToString());
        }
        
        private static Type GetTypeByName(string typeName)
            => AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(x => x.GetTypes())
                        .FirstOrDefault(x => x.Name == typeName);
    }
}