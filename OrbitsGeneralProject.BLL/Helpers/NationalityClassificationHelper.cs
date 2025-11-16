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
        private static readonly HashSet<int> NonGulfArabDialCodes = new(new[]
        {
            20,   // Egypt
            212,  // Morocco
            213,  // Algeria
            216,  // Tunisia
            218,  // Libya
            249,  // Sudan
            962,  // Jordan
            961,  // Lebanon
            963,  // Syria
            964,  // Iraq
            970,  // Palestine
            967,  // Yemen
            222,  // Mauritania
            252,  // Somalia
            253,  // Djibouti
            269   // Comoros
        });
        private static readonly HashSet<int> AllArabDialCodes = new(GulfDialCodes.Concat(NonGulfArabDialCodes));

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

        private static readonly string[] GeneralArabKeywordsEnglish =
        {
            "morocco",
            "algeria",
            "tunisia",
            "libya",
            "sudan",
            "jordan",
            "palestine",
            "gaza",
            "syria",
            "lebanon",
            "iraq",
            "yemen",
            "mauritania",
            "somalia",
            "djibouti",
            "comoros"
        };

        private static readonly string[] GeneralArabKeywordsArabic =
        {
            "مغرب",
            "جزائ",
            "تونس",
            "ليبيا",
            "سودان",
            "أردن",
            "فلسط",
            "غزه",
            "سوري",
            "لبنان",
            "عراق",
            "يمن",
            "موريت",
            "صومال",
            "جيبوت",
            "جزر القمر",
            "قمر"
        };

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

        public static bool IsArabNationality(Nationality? nationality)
        {
            if (nationality == null)
                return false;

            if (IsEgyptian(nationality) || IsGulf(nationality))
                return true;

            if (nationality.TelCode.HasValue && AllArabDialCodes.Contains(nationality.TelCode.Value))
                return true;

            if (string.IsNullOrWhiteSpace(nationality.Name))
                return false;

            var normalizedLower = nationality.Name.Trim().ToLowerInvariant();

            if (GeneralArabKeywordsEnglish.Any(keyword => normalizedLower.Contains(keyword)))
                return true;

            return GeneralArabKeywordsArabic.Any(keyword => normalizedLower.Contains(keyword));
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

        public static ResidentGroup? ResolveResidentGroup(Nationality? nationality)
        {
            if (nationality == null)
                return null;

            if (IsEgyptian(nationality))
                return ResidentGroup.Egyptian;

            if (IsArabNationality(nationality))
                return ResidentGroup.Arab;

            return ResidentGroup.Foreign;
        }
    }
}

