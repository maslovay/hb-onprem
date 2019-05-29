using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using StackRedis.L1.Notifications;

namespace MemoryCacheService
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
            where T : class
        {
            var ser = JsonConvert.SerializeObject(newObject).ToString();
            _memoryDatabase.StringSetAsync(id.ToString(), ser);
        }

        /// <summary>
        /// Gets last object of type T from a database
        /// Returns KeyValuePair
        /// If key GUID == Guid.Empty, the operation if failed and you should try again
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public KeyValuePair<Guid, T> Dequeue<T>()
            where T : class
        {
            var keys = _server.Keys().Reverse();
            
            if (GetValue(keys, out KeyValuePair<Guid, T> keyValuePair, true) != default(RedisKey)) 
                return keyValuePair;

            return new KeyValuePair<Guid, T>(Guid.Empty, default(T));
        }

        private RedisKey GetValue<T>(IEnumerable<RedisKey> keys, out KeyValuePair<Guid, T> keyValuePair, bool deleteKey) where T : class
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
                    var origValue = JsonConvert.DeserializeObject<T>(RedisValue.Unbox(val));

                    if (deleteKey)
                        _memoryDatabase.KeyDelete(key);
                    
                    keyValuePair = new KeyValuePair<Guid, T>(Guid.Parse(key), (T) origValue);
                    return key;
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            
            keyValuePair = new KeyValuePair<Guid, T>(Guid.Empty, default(T));
            return default(RedisKey);
        }

        /// <summary>
        /// Dequeue with a condition
        /// </summary>
        /// <param name="expr"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public KeyValuePair<Guid, T> Dequeue<T>(Func<T,bool> expr)
            where T : class
        {
            var ret = new KeyValuePair<Guid, T>( Guid.Empty, default(T) );
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
    }
}