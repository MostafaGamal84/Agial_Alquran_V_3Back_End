namespace Orbits.GeneralProject.Core.Infrastructure
{
    public static class BusinessDateTime
    {
        private static readonly Lazy<TimeZoneInfo> CairoTimeZoneResolver = new(ResolveCairoTimeZone);
        private static readonly Lazy<TimeZoneInfo> SaudiTimeZoneResolver = new(ResolveSaudiTimeZone);

        public static TimeZoneInfo CairoTimeZone => CairoTimeZoneResolver.Value;
        public static TimeZoneInfo SaudiTimeZone => SaudiTimeZoneResolver.Value;

        public static DateTime UtcNow => DateTime.UtcNow;

        public static DateTime CairoNow => NormalizeStoredLocalDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, CairoTimeZone));

        public static DateTime SaudiNow => NormalizeStoredLocalDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, SaudiTimeZone));

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
            return value.Kind switch
            {
                DateTimeKind.Unspecified => NormalizeStoredLocalDateTime(value),
                DateTimeKind.Utc => NormalizeStoredLocalDateTime(TimeZoneInfo.ConvertTimeFromUtc(value, CairoTimeZone)),
                _ => NormalizeStoredLocalDateTime(TimeZoneInfo.ConvertTime(value, CairoTimeZone))
            };
        }

        public static DateTime ToUtcFromCairoLocal(DateTime value)
        {
            return NormalizeStoredLocalDateTime(value);
        }

        public static DateTime NormalizeClientDateTimeToCairoStorage(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => ToCairo(value),
                DateTimeKind.Local => ToCairo(value),
                _ => ToCairoFromSaudiLocal(value)
            };
        }

        public static DateTime NormalizeClientDateTimeToUtc(DateTime value)
        {
            return NormalizeClientDateTimeToCairoStorage(value);
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCairoDayRangeUtc(DateTime cairoDate)
        {
            var localValue = NormalizeStoredLocalDateTime(cairoDate);
            var startLocal = new DateTime(localValue.Year, localValue.Month, localValue.Day, 0, 0, 0, DateTimeKind.Unspecified);
            return (startLocal, startLocal.AddDays(1));
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCurrentCairoMonthRangeUtc()
        {
            var nowInCairo = CairoNow;
            return GetCairoMonthRangeUtc(nowInCairo.Year, nowInCairo.Month);
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCairoMonthRangeUtc(int year, int month)
        {
            var startLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return (startLocal, startLocal.AddMonths(1));
        }

        public static bool IsInSameCairoMonth(DateTime firstValue, DateTime secondValue)
        {
            var firstInCairo = ToCairo(firstValue);
            var secondInCairo = ToCairo(secondValue);

            return firstInCairo.Year == secondInCairo.Year
                && firstInCairo.Month == secondInCairo.Month;
        }

        public static DateTime ToCairoFromSaudiLocal(DateTime value)
        {
            var saudiLocalValue = value.Kind == DateTimeKind.Unspecified
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

            var cairoValue = TimeZoneInfo.ConvertTime(saudiLocalValue, SaudiTimeZone, CairoTimeZone);
            return NormalizeStoredLocalDateTime(cairoValue);
        }

        public static DateTime NormalizeStoredLocalDateTime(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
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

        private static TimeZoneInfo ResolveSaudiTimeZone()
        {
            string[] preferredTimeZoneIds =
            {
                "Asia/Riyadh",
                "Arab Standard Time"
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
