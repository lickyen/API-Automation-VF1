using Hangfire;
using Hangfire.Dashboard;

namespace ProductivityApi.Helpers
{
    public class ISTTimeZoneResolver : ITimeZoneResolver
    {
        public static readonly TimeZoneInfo IST = GetIST();

        private static TimeZoneInfo GetIST()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        }

        public TimeZoneInfo GetLocal() => IST;

        public TimeZoneInfo GetTimeZoneById(string timeZoneId)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
    }
}