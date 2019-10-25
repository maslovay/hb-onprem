using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models.AnalyticModels
{
    public class ClientProfileModels
    {

        public class AgeBoarder
        {
            public int BegAge;
            public int EndAge;
        }

        public class GenderAgeStructureResult
        {
            public string Age { get; set; }
            public int MaleCount { get; set; }
            public int FemaleCount { get; set; }
            public double? MaleAverageAge { get; set; }
            public double? FemaleAverageAge { get; set; }
        }
    }
}
