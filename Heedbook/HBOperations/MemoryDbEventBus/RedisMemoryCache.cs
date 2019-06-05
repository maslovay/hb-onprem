using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
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

            ThreadPool.SetMinThreads(10, 10); // Against a TIMEOUT error
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
            var keys = _server.Keys().ToArray();

            if (GetValue(keys, out KeyValuePair<Guid, dynamic> keyValuePair, true) != default(RedisKey))
                return keyValuePair;

            return new KeyValuePair<Guid, dynamic>(Guid.Empty, null);
        }

        private RedisKey GetValue(IEnumerable<RedisKey> keys, out KeyValuePair<Guid, dynamic> keyValuePair,
            bool deleteKey)
        {
            keyValuePair = new KeyValuePair<Guid, dynamic>(Guid.Empty, null);
            foreach (var key in keys)
            {
                if (GetValueForKey(out keyValuePair, deleteKey, key, out var redisKey)) return redisKey;
            }

            return default(RedisKey);
        }

        private bool GetValueForKey(out KeyValuePair<Guid, dynamic> keyValuePair, bool deleteKey, RedisKey key, out RedisKey redisKey)
        {
            keyValuePair = new KeyValuePair<Guid, dynamic>(Guid.Empty, null);
            
            if (key.Equals(default(RedisKey)))
                return false;

            var val = _memoryDatabase.StringGet(key);
            if (!val.HasValue)
                return false;

            try
            {
                var origValue = JsonConvert.DeserializeObject(RedisValue.Unbox(val));

                if (deleteKey)
                    _memoryDatabase.KeyDelete(key);

                keyValuePair = new KeyValuePair<Guid, dynamic>(Guid.Parse(key), (dynamic) origValue);
                {
                    redisKey = key;
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Dequeue with a condition
        /// </summary>
        /// <param name="expr"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public KeyValuePair<Guid, dynamic> Dequeue(Func<dynamic, bool> expr)
        {
            var ret = new KeyValuePair<Guid, dynamic>(Guid.Empty, null);
            var keys = _server.Keys().ToArray();
            RedisKey key;

            while (ret.Value == null || !expr(ret.Value))
                key = GetValue(keys, out ret, false);

            if (key != default(RedisKey))
                _memoryDatabase.KeyDelete(key);

            return ret;
        }


        public void Clear()
        {
            try
            {
                _server.FlushDatabase(_memoryDatabase.Database);
            }
            catch (RedisCommandException ex)
            {
                var keys = _server.Keys();

                foreach (var key in keys)
                    _memoryDatabase.KeyDelete(key);
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                _server.FlushDatabase(_memoryDatabase.Database);
            }
            catch (RedisCommandException ex)
            {
                var keys = _server.Keys();

                foreach (var key in keys)
                    _memoryDatabase.KeyDeleteAsync(key);
            }
        }

        public int Count()
            => _server.Keys().Count();

        public bool HasRecords()
            => _server.Keys().Any();

        public bool HasRecords<T>(Func<T, bool> expr)
        {
                var ret = new KeyValuePair<Guid, dynamic>(Guid.Empty, null);

                foreach (var key in _server.Keys())
                {
                    try
                    {
                        GetValueForKey(out var keyValuePair, false, key, out var redisKey);

                        if (keyValuePair.Key == Guid.Empty || keyValuePair.Value == null)
                            continue;

                        var deserialized = JsonConvert.DeserializeObject<T>(keyValuePair.Value.ToString());

                        if (expr(deserialized))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        // doing nothing
                    }
                }

                return false;
        }
    }
}