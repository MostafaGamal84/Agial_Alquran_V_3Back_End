using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Orbits.GeneralProject.BLL.StaticEnums
{
    public enum LanguagesEnum
    {
        [Display(Name = "en")]
        EN = 1,
        [Display(Name = "ar")]
        AR = 2,
        [Display(Name = "gr")]
        GR = 3
    }
}
