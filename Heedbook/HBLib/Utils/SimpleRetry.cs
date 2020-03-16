using System;
using System.Collections.Generic;
using System.Threading;

namespace HBLib.Utils
{
    public static class Retry
    {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            Int32 maxAttemptCount = 3)
        {
            Do<Object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            Int32 maxAttemptCount = 3,
            Boolean isProgressiveTimeout = true)
        {
            var exceptions = new List<Exception>();

            for (var attempted = 0; attempted < maxAttemptCount; attempted++)
                try
                {
                    if (attempted > 0)
                    {
                        var retryTimeout = isProgressiveTimeout
                            ? TimeSpan.FromSeconds(retryInterval.Seconds * attempted)
                            : retryInterval;
                        Thread.Sleep(retryInterval);
                    }

                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

            throw new AggregateException(exceptions);
        }
    }
}