using System;
using System.Collections.Generic;

namespace HBLib.Utils {
    public static class Retry {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3) {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3,
            bool isProgressiveTimeout= true) {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++) {
                try {
                    if (attempted > 0) {
                        var retryTimeout = isProgressiveTimeout ? TimeSpan.FromSeconds(retryInterval.Seconds * attempted) : retryInterval;
                        System.Threading.Thread.Sleep(retryInterval);
                    }
                    return action();
                } catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
