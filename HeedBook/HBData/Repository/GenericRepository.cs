using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Remotion.Linq.Clauses;

namespace HBData.Repository
{
    public class GenericRepository : IGenericRepository
    {
        private readonly RecordsContext _context;

        public GenericRepository(RecordsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<T>> FindAllAsync<T>() where T : class
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<T> FindOneByConditionAsync<T>(Expression<Func<T, Boolean>> predicate) where T : class
        {
            return await _context.Set<T>().Where(predicate).FirstOrDefaultAsync();
        }
        
        public async Task<IEnumerable<T>> FindByConditionAsync<T>(Expression<Func<T, Boolean>> predicate) where T : class
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public IEnumerable<T> Get<T>() where T: class
        {
            return _context.Set<T>();
        }

        public IEnumerable<T> GetWithInclude<T>(Expression<Func<T, Boolean>> predicate, params Expression<Func<T, object>>[] children) where T : class
        {
            var dbSet = _context.Set<T>();
            children.ToList().ForEach(x=>dbSet.Include(x).Load());
            return dbSet.Where(predicate);
        }

        public void BulkInsert<T>(IEnumerable<T> entities) where T : class
        {
            _context.Set<T>().AddRange(entities);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }
        
        public async Task<Dictionary<TKey, TElement>> FindByConditionAsyncToDictionary<T, TKey, TElement>(
            Expression<Func<T, Boolean>> expression, Func<T, TKey> keySelector,
            Func<T, TElement> elementSelector)
            where T : class
        {
            return await _context.Set<T>().Where(expression).ToDictionaryAsync(keySelector, elementSelector);
        }

        public void Create<T>(T entity) where T : class
        {
            _context.Set<T>().Add(entity);
        }

        public async Task CreateAsync<T>(T entity) where T : class
        {
            await _context.Set<T>().AddAsync(entity);
        }

        public void Update<T>(T entity) where T : class
        {
            _context.Set<T>().Update(entity);
        }

        public async void AddOrUpdate<T>(T entity, Expression<Func<T, Boolean>> predicateExpression) where T : class
        {
            if (await _context.Set<T>().AnyAsync(predicateExpression))
            {
                _context.Set<T>().Update(entity);
            }

            await _context.Set<T>().AddAsync(entity);
        }

        public async void AddOrUpdate<T>(T entity) where T : class
        {
            var properties = typeof(T).GetProperties();
            var keyValue = Guid.Empty;
            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<KeyAttribute>() == null) continue;
                keyValue = Guid.Parse(property.GetValue(entity).ToString());
                break;
            }

            if (keyValue == Guid.Empty)
            {
                throw new Exception($"{typeof(T).FullName} does not have key attribute");
            }

            if (_context.Set<T>().Find(keyValue) != null)
            {
                _context.Set<T>().Update(entity);
            }

            await _context.Set<T>().AddAsync(entity);
        }
        
        public void Delete<T>(T entity) where T : class
        {
            _context.Set<T>().Remove(entity);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}