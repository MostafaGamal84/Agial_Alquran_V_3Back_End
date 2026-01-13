using System;
using System.Collections.Generic;
using System.Linq;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;

namespace Orbits.GeneralProject.BLL.Helpers
{
    public static class NationalityClassificationHelper
    {
        private static readonly string[] EgyptianKeywordsEnglish =
        {
            "egypt",
            "egyptian"
        };

        private static readonly string[] EgyptianKeywordsArabic =
        {
            "مصر",
            "مصري",
            "مصرى"
        };

        private static readonly string[] GulfKeywordsEnglish =
        {
            "saudi",
            "ksa",
            "kuwait",
            "emirates",
            "uae",
            "bahrain",
            "oman",
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
            "comoros",
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
            //"ردن",
            "عمان",
            "مغرب",
            "جزائ",
            "تونس",
            "ليبيا",
            "سودان",
            "ردن",
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

        public static bool IsEgyptian(Nationality? nationality)
        {
            if (nationality == null)
                return false;

            if (string.IsNullOrWhiteSpace(nationality.Name))
                return false;

            var normalizedLower = nationality.Name.Trim().ToLowerInvariant();

            if (EgyptianKeywordsEnglish.Any(keyword => normalizedLower.Contains(keyword)))
                return true;

            return EgyptianKeywordsArabic.Any(keyword => normalizedLower.Contains(keyword));
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
            "ردن",
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

        private static readonly string[] ForeignKeywordsEnglish =
        {
            "usa",
            "united states",
            "american",
            "canada",
            "canadian",
            "uk",
            "united kingdom",
            "british",
            "england",
            "france",
            "french",
            "germany",
            "german",
            "italy",
            "italian",
            "spain",
            "spanish",
            "australia",
            "australian",
            "india",
            "indian",
            "pakistan",
            "pakistani",
            "bangladesh",
            "china",
            "chinese",
            "japan",
            "japanese",
            "philippines",
            "filipino",
            "indonesia",
            "indonesian",
            "malaysia",
            "malaysian",
            "russia",
            "russian",
            "turkey",
            "turkish",
            "iran",
            "iranian",
            "ethiopia",
            "ethiopian"
        };

        private static readonly string[] ForeignKeywordsArabic =
        {
            "أمريكا",
            "أمريكي",
            "كندا",
            "كندي",
            "بريط",
            "إنجل",
            "فرن",
            "ألمان",
            "إيطال",
            "إسبان",
            "أسترال",
            "هند",
            "باكستان",
            "بنغل",
            "صين",
            "يابان",
            "فلبين",
            "إندونيس",
            "ماليز",
            "روس",
            "تركي",
            "إيران",
            "أثيوب"
        };

        public static bool IsGulf(Nationality? nationality)
        {
            if (nationality == null)
                return false;

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

            if (string.IsNullOrWhiteSpace(nationality.Name))
                return false;

            var normalizedLower = nationality.Name.Trim().ToLowerInvariant();

            if (GeneralArabKeywordsEnglish.Any(keyword => normalizedLower.Contains(keyword)))
                return true;

            return GeneralArabKeywordsArabic.Any(keyword => normalizedLower.Contains(keyword));
        }

        public static bool IsForeign(Nationality? nationality)
        {
            if (nationality == null)
                return false;

            if (string.IsNullOrWhiteSpace(nationality.Name))
                return false;

            var normalizedLower = nationality.Name.Trim().ToLowerInvariant();

            if (ForeignKeywordsEnglish.Any(keyword => normalizedLower.Contains(keyword)))
                return true;

            return ForeignKeywordsArabic.Any(keyword => normalizedLower.Contains(keyword));
        }

        public static SubscribeForEnum? ResolveSubscribeFor(Nationality? nationality)
        {
            if (nationality == null)
                return null;

            if (IsEgyptian(nationality))
                return SubscribeForEnum.Egyptian;

            if (IsGulf(nationality))
                return SubscribeForEnum.Gulf;

            if (IsForeign(nationality))
                return SubscribeForEnum.NonArab;

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

            if (IsForeign(nationality))
                return ResidentGroup.Foreign;

            return ResidentGroup.Foreign;
        }
    }
}
