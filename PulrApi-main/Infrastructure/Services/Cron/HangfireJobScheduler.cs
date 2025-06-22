using Hangfire;
using Core.Application.Interfaces;

namespace Core.Infrastructure.Services.Cron
{
    public class HangfireJobScheduler
    {
        public static void ScheduleRecurringJobs()
        {
            RecurringJob.AddOrUpdate<IExchangeRateService>(nameof(IExchangeRateService), job => job.GetExchangeRates(), HourInterval(12));
        }

        public static string HourInterval(int interval)
        {
            return string.Format("0 */{0} * * *", (object)interval);
        }
    }
}
