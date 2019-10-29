using System.Collections.Generic;
using Tests.Models;

namespace Tests.TestedClass
{
    public class NewString
    {
        private IRepository _repo;
        public NewString(IRepository repo)
        {
            _repo = repo;            
        }
        public string SumStrings()
        {
            var result = "";
            var strings = _repo.GetAll();
            foreach(var str in strings)
                result += $" {str}";
            return result;
        }
        public string GetString()
        {
            var str = _repo.GetById(2);
            return str;
        }
    }
}