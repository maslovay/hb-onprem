using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nest;

namespace ErrorKibanaScheduler
{
    public class TextCompare
    {
        public double CompareText(string firstLog, string secondLog)
        {   
            var firstList = firstLog.Split(' ').ToList();
            var secondList = secondLog.Split(' ').ToList();
            double count;
            if (firstList.Count <= secondList.Count)
            {
                count = firstList.Where((t, i) => t == secondList[i]).Count();
            }
            else
            {
                count = secondList.Where((t, i) => t == firstList[i]).Count();
            }
            var res = count / secondList.Count * 100;
            return res;
        }

        public string FindMainError(string str)
        {
            try
            {
                str = str.Substring(str.IndexOf('}') + 1);
                str = str.Remove(str.IndexOf(" at "));
                return str;
            }
            catch
            {
                return str.Take(150).ToString();
            }
        }
    }
}