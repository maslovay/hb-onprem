using System;
using System.Reflection;

namespace UnitTestExtensions
{
    public static class UnitTestDetector
    {
        static UnitTestDetector()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.FullName.ToLowerInvariant().StartsWith("nunit.framework")) 
                    continue;
               
                IsRunningFromNUnit = true;
                break;
            }
        }

        public static bool IsRunningFromNUnit { get; } = false;
    }
}