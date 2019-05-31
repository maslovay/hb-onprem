using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace MemoryDbEventBus
{

    /// <summary>
    /// This class is a wrapper for memory cache operations in Redis
    /// </summary>
    public class RedisMemoryCache : IMemoryCache
    {
        private IDatabase _memoryDatabase;
        private IServer _server;

        public RedisMemoryCache(string connString)
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect(connString);
            _memoryDatabase = connectionMultiplexer.GetDatabase();
            _server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());
        }

        /// <summary>
        /// Puts a new value into a database
        /// </summary>
        /// <param name="id">Guid</param>
        /// <param name="newObject">Object</param>
        /// <typeparam name="T"></typeparam>
        public void Enqueue<T>(Guid id, T newObject)
            where T : IMemoryDbEvent
        {
            var ser = JsonConvert.SerializeObject(newObject).ToString();
            _memoryDatabase.StringSetAsync(id.ToString(), ser);
        }

        public void Enqueue(Guid id, JObject jObject)
        {
            _memoryDatabase.StringSetAsync(id.ToString(), jObject.ToString());
        }

        /// <summary>
        /// Gets last object of type T from a database
        /// Returns KeyValuePair
        /// If key GUID == Guid.Empty, the operation if failed and you should try again
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
//        public KeyValuePair<Guid, T> Dequeue<T>()
//            where T : IMemoryDbEvent
//        {
//
//        }

        public KeyValuePair<Guid, dynamic> Dequeue()
        {
            var keys = _server.Keys().Reverse();
            
            if (GetValue(keys, out KeyValuePair<Guid, dynamic> keyValuePair, true) != default(RedisKey)) 
                return keyValuePair;

            return new KeyValuePair<Guid, dynamic>(Guid.Empty, null);
        }

        private RedisKey GetValue(IEnumerable<RedisKey> keys, out KeyValuePair<Guid, dynamic> keyValuePair, bool deleteKey) 
        {
            foreach (var key in keys)
            {
                if (key.Equals(default(RedisKey)))
                    continue;

                var val = _memoryDatabase.StringGet(key);
                if (!val.HasValue)
                    continue;

                try
                {
                    var origValue = JsonConvert.DeserializeObject(RedisValue.Unbox(val));

                    if (deleteKey)
                        _memoryDatabase.KeyDelete(key);
                    
                    keyValuePair = new KeyValuePair<Guid, dynamic>(Guid.Parse(key), (dynamic) origValue);
                    return key;
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            
            keyValuePair = new KeyValuePair<Guid, dynamic>(Guid.Empty, null);
            return default(RedisKey);
        }

        /// <summary>
        /// Dequeue with a condition
        /// </summary>
        /// <param name="expr"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public KeyValuePair<Guid, dynamic> Dequeue(Func<dynamic,bool> expr)
        {
            var ret = new KeyValuePair<Guid, dynamic>( Guid.Empty, null );
            var keys = _server.Keys().Reverse().ToArray();
            RedisKey key;
            
            while (ret.Value == null || !expr(ret.Value))
                key = GetValue(keys, out ret, false);
            
            if (key != default(RedisKey))
                _memoryDatabase.KeyDelete(key);
            
            return ret;
        }


        public void Clear()
        {
            var keys = _server.Keys();

            foreach (var key in keys)
                _memoryDatabase.KeyDelete(key);
        }
        
        public async Task ClearAsync()
        {
            var keys = _server.Keys();

            foreach (var key in keys)
                _memoryDatabase.KeyDeleteAsync(key);
        }

        public int Count()
            => _server.Keys().Count();

        public bool HasRecords()
            => _server.Keys().Any();
    }
}