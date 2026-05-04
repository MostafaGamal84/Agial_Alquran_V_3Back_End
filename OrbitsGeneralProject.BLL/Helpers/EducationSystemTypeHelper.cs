using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Enums;

namespace Orbits.GeneralProject.BLL.Helpers
{
    public static class EducationSystemTypeHelper
    {
        public static EducationSystemType? Parse(int? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return Enum.IsDefined(typeof(EducationSystemType), value.Value)
                ? (EducationSystemType)value.Value
                : null;
        }

        public static bool SupportsQuran(int? value)
        {
            var parsed = Parse(value);
            return !parsed.HasValue
                || parsed == EducationSystemType.QuranSchool
                || parsed == EducationSystemType.Both;
        }

        public static bool SupportsAcademic(int? value)
        {
            var parsed = Parse(value);
            return parsed == EducationSystemType.AcademicSchool
                || parsed == EducationSystemType.Both;
        }

        public static bool CanAccessAcademicModule(int? userTypeId, int? educationSystemTypeId)
        {
            var parsedUserType = userTypeId.HasValue && Enum.IsDefined(typeof(UserTypesEnum), userTypeId.Value)
                ? (UserTypesEnum)userTypeId.Value
                : (UserTypesEnum?)null;

            return parsedUserType == UserTypesEnum.Admin
                || parsedUserType == UserTypesEnum.BranchLeader
                || SupportsAcademic(educationSystemTypeId);
        }
    }
}
