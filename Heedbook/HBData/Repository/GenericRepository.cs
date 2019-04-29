using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

        public async Task<IEnumerable<T>> FindByConditionAsync<T>(Expression<Func<T, Boolean>> predicate)
            where T : class
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public IEnumerable<T> Get<T>() where T : class
        {
            return _context.Set<T>();
        }

        public IEnumerable<T> GetWithInclude<T>(Expression<Func<T, Boolean>> predicate,
            params Expression<Func<T, Object>>[] children) where T : class
        {
            var dbSet = _context.Set<T>();
            children.ToList().ForEach(x => dbSet.Include(x).Load());
            return dbSet.Where(predicate);
        }

        public T GetWithIncludeOne<T>(Expression<Func<T, Boolean>> predicate,
            params Expression<Func<T, Object>>[] children) where T : class
        {
            var dbSet = _context.Set<T>();
            children.ToList().ForEach(x => dbSet.Where(predicate).Include(x).Load());
            return dbSet.First();
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
            if (await _context.Set<T>().AnyAsync(predicateExpression)) _context.Set<T>().Update(entity);

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

            if (keyValue == Guid.Empty) throw new Exception($"{typeof(T).FullName} does not have key attribute");

            if (_context.Set<T>().Find(keyValue) != null) _context.Set<T>().Update(entity);

            await _context.Set<T>().AddAsync(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Set<T>().Remove(entity);
        }

        public IEnumerable<Object> ExecuteDbCommand(List<String> properties, String sql,
            Dictionary<String, Object> @params = null)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                var instance = CreateType(properties, out var type);
                cmd.CommandText = sql;
                if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();

                if (@params != null)
                    foreach (var p in @params)
                    {
                        var dbParameter = cmd.CreateParameter();
                        dbParameter.ParameterName = p.Key;
                        dbParameter.Value = p.Value;
                        cmd.Parameters.Add(dbParameter);
                    }

                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        for (var fieldCount = 0; fieldCount < dataReader.FieldCount; fieldCount++)
                        {
                            type.GetRuntimeProperties().ToList()[fieldCount].SetValue(instance, dataReader[fieldCount]);
                        }

                        yield return instance;
                    }
                }
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        private Object CreateType(List<String> properties, out Type type)
        {
            //TODO: replace it and make generic.
            var asmName = new AssemblyName {Name = "MyDynamicAssembly"};
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
            var modBuilder =
                asmBuilder.DefineDynamicModule(asmName.Name);

            var typeBuilder = modBuilder.DefineType("CustomType",
                TypeAttributes.Public);

            MethodAttributes getSetAttr =
                MethodAttributes.Public | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig;
            foreach (var property in properties)
            {
                FieldBuilder customerNameBldr = typeBuilder.DefineField($"{property.ToLowerInvariant()}",
                    typeof(string),
                    FieldAttributes.Private);

                var propertyBuilder =
                    typeBuilder.DefineProperty(property, PropertyAttributes.HasDefault, typeof(Double), null);
                var custNameGetPropMthdBldr =
                    typeBuilder.DefineMethod($"get_{property}",
                        getSetAttr,
                        typeof(Double),
                        Type.EmptyTypes);
                ILGenerator custNameGetIL = custNameGetPropMthdBldr.GetILGenerator();

                custNameGetIL.Emit(OpCodes.Ldarg_0);
                custNameGetIL.Emit(OpCodes.Ldfld, customerNameBldr);
                custNameGetIL.Emit(OpCodes.Ret);

                MethodBuilder custNameSetPropMthdBldr =
                    typeBuilder.DefineMethod($"set_{property}",
                        getSetAttr,
                        null,
                        new Type[] {typeof(Double)});
                ILGenerator custNameSetIL = custNameSetPropMthdBldr.GetILGenerator();
                custNameSetIL.Emit(OpCodes.Ldarg_0);
                custNameSetIL.Emit(OpCodes.Ldarg_1);
                custNameSetIL.Emit(OpCodes.Stfld, customerNameBldr);
                custNameSetIL.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(custNameGetPropMthdBldr);
                propertyBuilder.SetSetMethod(custNameSetPropMthdBldr);
            }

            type = typeBuilder.CreateType();
            return Activator.CreateInstance(type);
        }
    }
}