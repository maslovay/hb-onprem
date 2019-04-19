using System;

namespace HBLib.Utils
{
    public static class ArrayExtensions
    {
        public static bool isMatch(this byte[] x, byte[] y, int index) 
        {
            for (int j = 0; j < y.Length; ++j)
                if (!x[j + index].Equals(y[j]))
                    return false;
            return true;
        }
    }
}