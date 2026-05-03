namespace Orbits.GeneralProject.Core.Infrastructure
{
    public static class BusinessDateTime
    {
        private static readonly Lazy<TimeZoneInfo> CairoTimeZoneResolver = new(ResolveCairoTimeZone);
        private static readonly Lazy<TimeZoneInfo> SaudiTimeZoneResolver = new(ResolveSaudiTimeZone);

        public const string CairoTimeZoneId = "Africa/Cairo";
        public const string SaudiTimeZoneId = "Asia/Riyadh";

        public static TimeZoneInfo CairoTimeZone => CairoTimeZoneResolver.Value;
        public static TimeZoneInfo SaudiTimeZone => SaudiTimeZoneResolver.Value;

        public static DateTime UtcNow => DateTime.UtcNow;

        public static DateTime CairoNow => ConvertUtcToTimeZone(UtcNow, CairoTimeZone);

        public static DateTime SaudiNow => ConvertUtcToTimeZone(UtcNow, SaudiTimeZone);

        public static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public static DateTime EnsureUnspecified(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        }

        public static DateTime ToCairo(DateTime value)
        {
            return ConvertUtcToTimeZone(EnsureUtc(value), CairoTimeZone);
        }

        public static DateTime ToSaudi(DateTime value)
        {
            return ConvertUtcToTimeZone(EnsureUtc(value), SaudiTimeZone);
        }

        public static DateTime ConvertUtcToTimeZone(DateTime utcValue, TimeZoneInfo timeZone)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utcValue), timeZone);
        }

        public static DateTime ConvertLocalTimeToUtc(DateTime localValue, TimeZoneInfo timeZone)
        {
            var unspecifiedLocal = EnsureUnspecified(localValue);
            var normalizedLocal = MoveToFirstValidLocalTime(unspecifiedLocal, timeZone);
            return TimeZoneInfo.ConvertTimeToUtc(normalizedLocal, timeZone);
        }

        public static DateTime ToUtcFromCairoLocal(DateTime value)
        {
            return ConvertLocalTimeToUtc(value, CairoTimeZone);
        }

        public static DateTime ToUtcFromSaudiLocal(DateTime value)
        {
            return ConvertLocalTimeToUtc(value, SaudiTimeZone);
        }

        public static DateTime NormalizeClientDateTimeToCairoStorage(DateTime value)
        {
            return NormalizeClientDateTimeToUtc(value);
        }

        public static DateTime NormalizeClientDateTimeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => ToUtcFromSaudiLocal(value)
            };
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetDayRangeUtc(DateTime localDate, TimeZoneInfo timeZone)
        {
            var normalizedLocal = EnsureUnspecified(localDate);
            var startLocal = new DateTime(normalizedLocal.Year, normalizedLocal.Month, normalizedLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var endLocal = startLocal.AddDays(1);

            return (ConvertLocalTimeToUtc(startLocal, timeZone), ConvertLocalTimeToUtc(endLocal, timeZone));
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetMonthRangeUtc(int year, int month, TimeZoneInfo timeZone)
        {
            var startLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endLocal = startLocal.AddMonths(1);

            return (ConvertLocalTimeToUtc(startLocal, timeZone), ConvertLocalTimeToUtc(endLocal, timeZone));
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCairoDayRangeUtc(DateTime cairoDate)
        {
            return GetDayRangeUtc(cairoDate, CairoTimeZone);
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCurrentCairoMonthRangeUtc()
        {
            var nowInCairo = CairoNow;
            return GetCairoMonthRangeUtc(nowInCairo.Year, nowInCairo.Month);
        }

        public static (DateTime StartUtc, DateTime EndUtc) GetCairoMonthRangeUtc(int year, int month)
        {
            return GetMonthRangeUtc(year, month, CairoTimeZone);
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
            var utcValue = ToUtcFromSaudiLocal(value);
            return ToCairo(utcValue);
        }

        public static DateTime NormalizeStoredLocalDateTime(DateTime value)
        {
            return EnsureUtc(value);
        }

        private static DateTime MoveToFirstValidLocalTime(DateTime localValue, TimeZoneInfo timeZone)
        {
            if (!timeZone.IsInvalidTime(localValue))
            {
                return localValue;
            }

            var adjustedValue = localValue;

            // Some business ranges start exactly at local midnight. When DST starts at 00:00
            // (for example Cairo on Friday, April 24, 2026), that wall-clock time does not exist.
            // Move forward until we reach the first valid local instant instead of throwing.
            while (timeZone.IsInvalidTime(adjustedValue))
            {
                adjustedValue = adjustedValue.AddMinutes(1);
            }

            return adjustedValue;
        }

        private static TimeZoneInfo ResolveCairoTimeZone()
        {
            string[] preferredTimeZoneIds =
            {
                CairoTimeZoneId,
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
                SaudiTimeZoneId,
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
