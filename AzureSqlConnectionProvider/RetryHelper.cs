namespace Dnn.AzureSqlConnectionProvider
{
    using System;
    using System.Configuration;
    using System.Diagnostics;

    using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

    public class RetryHelper
    {
        private static RetryPolicy _retryPolicy = null;

        public static RetryPolicy GetRetryPolicy()
        {
            if (_retryPolicy == null)
            {
                var maxRetryStr = ConfigurationManager.AppSettings["SqlRetry-MaxRetry"];
                int maxRetry;
                if (!int.TryParse(maxRetryStr, out maxRetry))
                {
                    maxRetry = 5;
                }

                var minBackOffStr = ConfigurationManager.AppSettings["SqlRetry-MinBackOff"];
                int minBackOff;
                if (!int.TryParse(minBackOffStr, out minBackOff))
                {
                    minBackOff = 200;
                }

                var maxBackOffStr = ConfigurationManager.AppSettings["SqlRetry-MaxBackOff"];
                int maxBackOff;
                if (!int.TryParse(maxBackOffStr, out maxBackOff))
                {
                    maxBackOff = 5000;
                }

                var deltaBackOffStr = ConfigurationManager.AppSettings["SqlRetry-DeltaBackOff"];
                int deltaBackOff;
                if (!int.TryParse(deltaBackOffStr, out deltaBackOff))
                {
                    deltaBackOff = 300;
                }

                var retryStrategy = new ExponentialBackoff(maxRetry, TimeSpan.FromMilliseconds(minBackOff),
                    TimeSpan.FromMilliseconds(maxBackOff), TimeSpan.FromMilliseconds(deltaBackOff));
                _retryPolicy = new RetryPolicy<SqlDatabaseTransientErrorDetectionStrategy>(retryStrategy);

                _retryPolicy.Retrying += (sender, args) =>
                {
                    // Log details of the retry.
                    var msg = string.Format("Retry - Count:{0}, Delay:{1}, Exception:{2}",
                        args.CurrentRetryCount, args.Delay, args.LastException);
                    Trace.WriteLine(msg, "Information");
                };
            }

            return _retryPolicy;
        }
    }
}
