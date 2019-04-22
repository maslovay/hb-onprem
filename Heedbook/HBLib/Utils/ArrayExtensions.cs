using System;

namespace HBLib.Utils
{
    public static class ArrayExtensions
    {
        /// <summary>
        ///     Detects if array match to another array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Boolean isMatch(this Byte[] x, Byte[] y, Int32 index)
        {
            for (var j = 0; j < y.Length; ++j)
                if (!x[j + index].Equals(y[j]))
                    return false;
            return true;
        }
    }
}