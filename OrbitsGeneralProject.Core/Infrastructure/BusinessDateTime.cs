namespace Orbits.GeneralProject.Core.Infrastructure
{
    public static class BusinessDateTime
    {
        private static readonly Lazy<TimeZoneInfo> CairoTimeZoneResolver = new(ResolveCairoTimeZone);

        public static TimeZoneInfo CairoTimeZone => CairoTimeZoneResolver.Value;

        public static DateTime UtcNow => DateTime.UtcNow;

        public static DateTime CairoNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, CairoTimeZone);

        public static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public static DateTime ToCairo(DateTime value)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(value), CairoTimeZone);
        }

        public static DateTime ToUtcFromCairoLocal(DateTime value)
        {
            var unspecifiedValue = value.Kind == DateTimeKind.Unspecified
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(unspecifiedValue, CairoTimeZone);
        }

        public static DateTime NormalizeClientDateTimeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => ToUtcFromCairoLocal(value)
            };
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCairoDayRangeUtc(DateTime cairoDate)
        {
            var startLocal = new DateTime(cairoDate.Year, cairoDate.Month, cairoDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            return (ToUtcFromCairoLocal(startLocal), ToUtcFromCairoLocal(startLocal.AddDays(1)));
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCurrentCairoMonthRangeUtc()
        {
            var nowInCairo = CairoNow;
            return GetCairoMonthRangeUtc(nowInCairo.Year, nowInCairo.Month);
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCairoMonthRangeUtc(int year, int month)
        {
            var startLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return (ToUtcFromCairoLocal(startLocal), ToUtcFromCairoLocal(startLocal.AddMonths(1)));
        }

        public static bool IsInSameCairoMonth(DateTime firstValue, DateTime secondValue)
        {
            var firstInCairo = ToCairo(firstValue);
            var secondInCairo = ToCairo(secondValue);

            return firstInCairo.Year == secondInCairo.Year
                && firstInCairo.Month == secondInCairo.Month;
        }

        private static TimeZoneInfo ResolveCairoTimeZone()
        {
            string[] preferredTimeZoneIds =
            {
                "Africa/Cairo",
                "Egypt Standard Time"
            };

            foreach (var timeZoneId in preferredTimeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
