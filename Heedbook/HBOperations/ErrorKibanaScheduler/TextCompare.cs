using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nest;

namespace ErrorKibanaScheduler
{
    public class TextCompare
    {

        string[] errorsArray = new string[]
        {
            "Collection was modified; enumeration operation may not execute",
            "FileNotFoundException: The specified path does not exist",
            "SshConnectionException: Client not connected",
            "An exception has been raised that is likely due to a transient failure",
            "ObjectDisposedException: Cannot access a disposed object",
            "The session is not open. at Renci.SshNet.SubsystemSession",
            "The connection pool has been exhausted, either raise MaxPoolSize",
            "FileNotFoundException: Could not find file",
            "System.Linq.Enumerable+EnumerablePartition`1[System.Char]",
            "SshOperationTimeoutException: Session operation has timed out",
            "Connection refused ---> System.Net.Sockets.SocketException"
        };
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

        public string ReplaceForMainError(string str)
        {
            var replacePhrase = errorsArray.Where(x => str.Contains(x)).FirstOrDefault();
            return replacePhrase ?? FindMainError(str);
        }

        private string FindMainError(string str)
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