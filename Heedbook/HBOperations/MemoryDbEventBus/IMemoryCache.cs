using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemoryDbEventBus
{
    public interface IMemoryCache
    {
        /// <summary>
        /// Puts a new value into a database
        /// </summary>
        /// <param name="id">Guid</param>
        /// <param name="newObject">Object</param>
        /// <typeparam name="T"></typeparam>
        void Enqueue<T>(Guid id, T newObject)
            where T : class;

        /// <summary>
        /// Gets last object of type T from a database
        /// Returns KeyValuePair
        /// If key GUID == Guid.Empty, the operation if failed and you should try again
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        KeyValuePair<Guid, T> Dequeue<T>()
            where T : class;

        KeyValuePair<Guid, T> Dequeue<T>(Func<T, bool> expr)
            where T : class;

        void Clear();

        Task ClearAsync();
        
        int Count();

        bool HasRecords();
    }
}