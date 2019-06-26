using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Common.Utils
{
    public static class ModelsFactory
    {
        public static T Generate<T>( Func<T, object> func )
        where T : class, new()
        {
            T obj = new T();
            func(obj);
            return obj;
        }
    }
}