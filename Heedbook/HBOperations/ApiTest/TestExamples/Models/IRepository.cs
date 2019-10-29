using System.Collections.Generic;

namespace Tests.Models
{
    public interface IRepository
    {
        IEnumerable<string> GetAll();
        string GetById(int id);
        void Create (string str);
    }
}