using System;
using System.Reflection;

namespace UnitTestExtensions
{
    public static class UnitTestDetector
    {
        private static readonly bool _runningFromNUnit = false;      

        static UnitTestDetector()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.FullName.ToLowerInvariant().StartsWith("nunit.framework")) 
                    continue;
               
                _runningFromNUnit = true;
                break;
            }
        }

        public static bool IsRunningFromNUnit
        {
            get
            {
                return _runningFromNUnit;
            }
        }
    }
}