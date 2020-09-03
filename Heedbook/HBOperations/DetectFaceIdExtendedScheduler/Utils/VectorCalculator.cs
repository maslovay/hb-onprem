using System;
using System.Collections.Generic;
using System.Linq;

namespace DetectFaceIdExtendedScheduler.Utils
{
    public class VectorCalculation
    {
        public double VectorNorm(List<double> vector)
        {
            return Math.Sqrt(vector.Sum(p => Math.Pow(p, 2) ));
        }

        public double? VectorMult(List<double> vector1, List<double> vector2)
        {
            if (vector1.Count() != vector2.Count()) return null;
            var result = 0.0;
            for (int i =0; i < vector1.Count(); i++)
            {   
                result += vector1[i] * vector2[i];
            }
            return result;
        }

        public double? Cos(List<double> vector1, List<double> vector2)
        {
            return VectorMult(vector1, vector2) / VectorNorm(vector1) / VectorNorm(vector2);
        }
    }
}