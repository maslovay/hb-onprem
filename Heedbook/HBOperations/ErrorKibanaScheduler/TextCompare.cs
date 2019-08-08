using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nest;

namespace ErrorKibanaScheduler
{
    public class TextCompare
    {
        public int CompareText(string firstLog, string secondLog)
        {
            if (firstLog.Length != secondLog.Length)
            {
                return 0;
            }
            var firstList = firstLog.Split(' ').ToList();
            var secondList = secondLog.Split(' ').ToList();
            var count = firstList.Where((t, i) => t == secondList[i]).Count();
            return secondList.Count / count * 100;
        }
    }
}