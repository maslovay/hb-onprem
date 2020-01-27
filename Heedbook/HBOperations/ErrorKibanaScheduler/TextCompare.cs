using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                "SshOperationTimeoutException: Session operation has timed out",
                "Connection refused ---> System.Net.Sockets.SocketException",               
                "Too many holes in dialogue"
        };

        //"Error dialogue.",
        //       "Exception occured with this input parameters",
        //       "Error with stt results for",
        //       "System.NullReferenceException: Object reference not set to an instance of an object",

        public double CompareText(string log1, string funcName1, string log2, string funcName2)
        {
            if (funcName1 != funcName2)
                return 0;
            if (log1 == log2)
                return 100;
            var firstList = log1.Split(' ').ToList();
            var secondList = log2.Split(' ').ToList();
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
            return replacePhrase ?? ReplacePath(str);
        }

        public string ReplaceGuids(string s)
        {
            Regex regex = new Regex(@"\w{8}-\w{4}-\w{4}-\w{4}-\w{12}", RegexOptions.IgnoreCase);
            return regex.Replace(s, "");
        }

        private string ReplacePath(string s)
        {
            Regex regex = new Regex(@"\{Path\}", RegexOptions.IgnoreCase);
            s = regex.Replace(s, "");
            regex = new Regex(@"\{DialogueId\}", RegexOptions.IgnoreCase);
            s = regex.Replace(s, "");
            try
            {
                s = s.Remove(s.IndexOf(" at "));
            }
            catch { }
            return String.Concat(s.Take(150));
        }
    }
}