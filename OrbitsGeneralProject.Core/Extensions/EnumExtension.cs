using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Orbits.GeneralProject.Core.Extensions
{
    public static class EnumExtensions
    {
        public static int IntValue( this Enum enumValue )
        {
            return Convert.ToInt32(enumValue);
        }

        public static string StringValue( this Enum enumValue )
        {
            return Convert.ToInt32(enumValue).ToString();
        }

        public static string GetDescription<T>( this T enumValue )
          where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                return null;

            var description = enumValue.ToString();
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            if (fieldInfo != null)
            {
                var attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    description = ((DescriptionAttribute)attrs [0]).Description;
                }
            }

            return description;
        }
    }

    public static class EnumHelper<T>
    {
        public static T GetValueFromName( string name )
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DisplayAttribute)) as DisplayAttribute;
                if (attribute != null)
                {
                    if (attribute.Name.ToLower() == name.ToLower())
                    {
                        return (T)field.GetValue(null);
                    }
                }
                else
                {
                    if (field.Name.ToLower() == name.ToLower())
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentOutOfRangeException("name");
        }
    }
}