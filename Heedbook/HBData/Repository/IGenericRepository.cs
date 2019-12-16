using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HBData.Models;

namespace HBData.Repository
{
    public interface IGenericRepository
    {      
        Task<IEnumerable<T>> FindAllAsync<T>()
            where T : class;

        Task<IEnumerable<T>> FindByConditionAsync<T>(Expression<Func<T, Boolean>> predicate)
            where T : class;

        Task<T> FindOrNullOneByConditionAsync<T>(Expression<Func<T, Boolean>> predicate)
            where T : class;

        Task<T> FindOrExceptionOneByConditionAsync<T>(Expression<Func<T, Boolean>> predicate) 
            where T : class;

        IEnumerable<T> Get<T>() where T : class;
        IQueryable<T> GetAsQueryable<T>() where T : class;

        IEnumerable<T> GetWithInclude<T>(Expression<Func<T, Boolean>> predicate,
            params Expression<Func<T, Object>>[] children)
            where T : class;
        IQueryable<T> GetWithIncludeAsQueryable<T>(Expression<Func<T, Boolean>> predicate,
            params Expression<Func<T, Object>>[] children) where T : class;
        
        T GetWithIncludeOne<T>(Expression<Func<T, Boolean>> predicate, params Expression<Func<T, Object>>[] children)
            where T : class;
     
        Task<Dictionary<TKey, TElement>> FindByConditionAsyncToDictionary<T, TKey, TElement>(
            Expression<Func<T, Boolean>> expression, Func<T, TKey> keySelector,
            Func<T, TElement> elementSelector)
            where T : class;

        void Create<T>(T entity)
            where T : class;

        Task CreateAsync<T>(T entity)
            where T : class;

        Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class;

        void BulkInsert<T>(IEnumerable<T> entities) where T : class;

        void Update<T>(T entity)
            where T : class;

        void AddOrUpdate<T>(T entity, Expression<Func<T, Boolean>> predicate) where T : class;

        void AddOrUpdate<T>(T entity) where T : class;

        void Delete<T>(Expression<Func<T, bool>> expr)
            where T : class;

        void Delete<T>(IEnumerable<T> list)
            where T : class;

        void Delete<T>(T entity)
            where T : class;

        IEnumerable<Object> ExecuteDbCommand(List<String> properties, String sql, Dictionary<String, Object> @params = null);

        void Save();
        void Dispose();

        Task SaveAsync();
    }
}