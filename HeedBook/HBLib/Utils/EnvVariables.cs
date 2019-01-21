using System;
using System.Collections;
using System.Collections.Generic;

namespace HBLib.Utils
{
    public class EnvVar
    {
        public static string Get(string name)
        {
            var res = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            if (res == null) throw new Exception($"Environment variable not found {name}");
            return res;
        }

        public static IDictionary<string, string> GetAll()
        {
            var res = new Dictionary<string, string>();
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                res[de.Key.ToString()] = de.Value.ToString();
            return res;
        }
    }
}