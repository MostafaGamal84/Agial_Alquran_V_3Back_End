using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Orbits.GeneralProject.BLL.StaticEnums
{
    public enum TableNameEnum
    {
        [Display(Name = "Branch")]
        Branch = 1 ,
        [Display(Name = "City")]
        City = 2 ,
        [Display(Name = "Company")]
        Company = 3 ,
        [Display(Name = "Country")]
        Country = 4 ,
        [Display(Name = "Currency")]
        Currency = 5 ,
        [Display(Name = "DecimalType")]
        DecimalType = 6 ,
        [Display(Name = "Department")]
        Department = 7 ,
        [Display(Name = "Governorate")]
        Governorate = 8 ,
        [Display(Name = "Industry")]
        Industry = 9 ,
        [Display(Name = "Organization")]
        Organization = 10 ,
        [Display(Name = "RoundingType")]
        RoundingType = 11 ,
        [Display(Name = "Tax")]
        Tax = 12 ,
        [Display(Name = "TaxClassification")]
        TaxClassification = 13 ,
        [Display(Name = "TaxType")]
        TaxType = 14 ,
        [Display(Name = "User")]
        User =  15,
        [Display(Name = "Page")]
        Page = 16
    }

}
