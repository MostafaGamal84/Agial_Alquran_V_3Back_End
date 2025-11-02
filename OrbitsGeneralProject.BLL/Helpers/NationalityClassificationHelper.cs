using System;
using System.Collections.Generic;
using System.Linq;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;

namespace Orbits.GeneralProject.BLL.Helpers
{
    public static class NationalityClassificationHelper
    {
        private static readonly HashSet<int> GulfDialCodes = new(new[] { 971, 966, 965, 974, 973, 968 });

        private static readonly string[] GulfKeywordsEnglish =
        {
            "saudi",
            "ksa",
            "kuwait",
            "emirates",
            "uae",
            "bahrain",
            "oman",
            "qatar"
        };

        private static readonly string[] GulfKeywordsArabic =
        {
            "سعود",
            "كويت",
            "امارات",
            "الإمارات",
            "بحرين",
            "قطر",
            "عمان"
        };

        public static bool IsEgyptian(Nationality? nationality)
        {
            if (nationality == null)
                return false;

            if (nationality.TelCode.HasValue && nationality.TelCode.Value == 20)
                return true;

            if (string.IsNullOrWhiteSpace(nationality.Name))
                return false;

            var normalizedLower = nationality.Name.Trim().ToLowerInvariant();

            if (normalizedLower.Contains("egypt"))
                return true;

            return normalizedLower.Contains("مصر")
                || normalizedLower.Contains("مصري")
                || normalizedLower.Contains("مصرى");
        }

        public static bool IsGulf(Nationality? nationality)
        {
            if (nationality == null)
                return false;

            if (nationality.TelCode.HasValue && GulfDialCodes.Contains(nationality.TelCode.Value))
                return true;

            if (string.IsNullOrWhiteSpace(nationality.Name))
                return false;

            var normalizedLower = nationality.Name.Trim().ToLowerInvariant();

            if (GulfKeywordsEnglish.Any(keyword => normalizedLower.Contains(keyword)))
                return true;

            return GulfKeywordsArabic.Any(keyword => normalizedLower.Contains(keyword));
        }

        public static SubscribeForEnum? ResolveSubscribeFor(Nationality? nationality)
        {
            if (nationality == null)
                return null;

            if (IsEgyptian(nationality))
                return SubscribeForEnum.Egyptian;

            if (IsGulf(nationality))
                return SubscribeForEnum.Gulf;

            return SubscribeForEnum.NonArab;
        }
    }
}

