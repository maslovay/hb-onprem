using System;
using System.Collections.Generic;
using System.Linq;

namespace DetectFaceIdScheduler.Utils
{
    public class VectorCalculation
    {
        public double VectorNorm(List<double> vector)
        {
            if (vector != null)
            {
                return Math.Sqrt(vector.Sum(p => Math.Pow(p, 2) ));
            }
            else
            {
                return 0;
            }
        }

        public double? VectorMult(List<double> vector1, List<double> vector2)
        {
            try
            {
                if (vector1 != null && vector2 != null)
                {
                    if (vector1.Count() != vector2.Count()) return null;
                    var result = 0.0;
                    for (int i =0; i < vector1.Count(); i++)
                    {   
                        result += vector1[i] * vector2[i];
                    }
                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch 
            {
                return null;
            }
        }

        public double? Cos(List<double> vector1, List<double> vector2)
        {
            if (vector1 != null && vector2 != null)
            {
                var norm1 = VectorNorm(vector1);
                var norm2 = VectorNorm(vector2);
                return (norm1 != 0 && norm2 != 0) ? VectorMult(vector1, vector2) / VectorNorm(vector1) / VectorNorm(vector2) : 0;
            }
            else
            {
                return 0;
            }
        }
    }
}