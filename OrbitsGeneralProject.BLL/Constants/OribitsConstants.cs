using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System.ComponentModel;

namespace Orbits.GeneralProject.BLL.Constants
{
    #region DbSchemas

    public class DXConstants
    {
        public static class DbSchemas
        {
            public const string GeneralSetting = "GeneralSetting";
            public const string StaticData = "StaticData";
            public const string Security = "Security";
            public const string Lang = "Lang";
            public static string ReferenceTableName = "[{0}].[{1}]";
        }

        #endregion DbSchemas
        #region Languages & Localization
        public static class SupportedLanguage
        {
            public const string RequestHeader = "Accept-Language";
            public const string EN = "en";
            public const string AR = "ar";
            public const int DefaultLangId = 1;
        }
        #endregion

        public static class Constanties
        {
            public const string ORDERASC = "ASC";
            public const string ORDERDESC = "DESC";


            public const string HAJJ_START_DAY = "10";
            public const string HAJJ_START_MONTH = "11";

            public const string HAJJ_END_DAY = "20";
            public const string HAJJ_END_MONTH = "12";

            public const string HIJRI_TYPE = "H";
            public const string GREGORIAN_TYPE = "G";
            public const string IMAGE_UPLOAD_PATH = "images/uploaded";
            public const string TempIncidentPageRoute = "TempIncident/Create/";
        }
    }
    
    #region LoginValidation
    public static class LoginValidationReponseConstants
    {
        public const string EmailNotNullOrEmpty = "البريد الإلكتروني مطلوب";
        public const string PasswordNotNullOrEmpty = "الرقم السري مطلوب";
        public const string PasswordMustBeComplex = "يجب ألا تقل كلمة المرور عن 8 أحرف، وتحتوي على الأقل على حرف كبير (A-Z)، وحرف صغير (a-z)، ورقم (0-9)، وحرف خاص مثل (@, #, $, %).";
        public const string ConfirmPasswordMustMatch = "كلمة المرور وتأكيدها غير متطابقين.";
        public const string CodeNullOrEmpty = "الكود لا يجب ان يكون فارغ";
        public const string CodeLength = "الكود مكون من 4 أرقام";
        public const string ChangePasswordCodeSent = "تم ارسال الكود الى بريدك، الرجاء التحقق من رسائل البريد";
        public const string ChangePasswordSuccess = "تم تغيير كلمة السر بنجاح";
    }
    #endregion
    #region UserValidationReponseConstants
    public static class UserValidationReponseConstants
    {
        public const string UserNameNotNullOrEmpty = "اسم المستخدم مطلوب!";
        public const string UserNameMaxLength = "عدد حروف اسم المستخدم لا يجب أن تتعدي 250 حرفا";
        public const string EmailNotNullOrEmpty = "البريد الإلكتروني مطلوب!";
        public const string NationalIdLength = "رقم الهوية يجب أن يكون 10 أرقام";
        public const string ValidNationalId = "رقم الهوية يجب أن يحتوي أرقام فقط";
        public const string PhoneNumberLength = "رقم الهاتف يجب أن يكون 10 أرقام";
        public const string ValidPhoneNumber = "رقم الهاتف يجب أن يحتوي أرقام فقط";
        public const string PasswordLength = "الرقم السري يجب أن لا يقل عن 8 أرقام أو حروف";
        public const string PasswordNotEmptyOrNull = "الرقم السري مطلوب";
        public const string CodeNotEmptyOrNull = "الكود مطلوب";
        public const string CodeLength = "الكود مكون من 4 أرقام";
        public const string WhiteSpace = "لا يسمح بالمساحة الفارغة في الحقول";
        public const string ValidEmail = "يجب إدخال بريد إلكتروني صحيح";
        public const string ValidUserName = "لا يجب وضع علامات خاصة في الإسم";
        public const string UserTypeRequired = "يجب إختيار نوع المستخدم";
    }
    #endregion
    #region DepartmentValidationResponse 
    public static class DepartmentValidationResponseConstants
    {
        public const string TYPE_NAME_Must_Not_Null = "الإسم لا يمكن اي يكون فارغا";
        public const string Type_Max_Length = "لا يمكن أن يزيد اسم الادارة عن 250 حرف !";
        public const string Type_Id_Must_Not_Null = "يجب التاكد من اختيار نوع الادارة";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
    }
    #endregion #region DepartmentValidationResponse 
    #region DepartmentTypeValidationResponseConstants
    public static class DepartmentTypeValidationResponseConstants
    {
        public const string TYPE_NAME_Must_Not_Null = "الإسم لا يمكن اي يكون فارغا";
        public const string Type_Max_Length = "لا يمكن أن يزيد اسم النوع عن 50 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string DepartmentTypeIdRequired = "يجب إختيار النوع الذي سيتم تعديله";
    }
    #endregion
    #region CenterTypeValidationResponseConstants
    public static class CenterTypeValidationResponseConstants
    {
        public const string TYPE_NAME_Must_Not_Null = "الإسم لا يمكن اي يكون فارغا";
        public const string Type_Max_Length = "لا يمكن أن يزيد اسم النوع عن 50 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string CenterTypeIdRequired = "يجب إختيار النوع الذي سيتم تعديله";
    }
    #endregion
    #region OverdueSettingValidationResponseConstants
    public static class OverdueSettingValidationResponseConstants
    {
        public const string PriorityLevelRequired = "درجة الخطورة مطلوبة";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string UserTypeIdRequired = "يجب إختيار نوع المستخدم الذي سيتم إبلاغه";
        public const string DaysRequired = "يجب اختيار الأيام";
        public const string DaysMustBeMoreThanZero = "يجب اختيار رقم اكبر من صفر في الأيام";
        public const string IdRequired = "يجب اختيار ";
        public const string StatusIdNotValid = "لا يمكن عمل إعدادات تصعيد لحالة تم التصعيد أو حالة تمت المعالجة النهائية أو حالة تم الإلغاء";
        public const string StatusIdRequired = "حالة البلاغ مطلوبة";
    }
    #endregion
    #region QuantityTypeValidationResponseConstants
    public static class QuantityTypeValidationResponseConstants
    {
        public const string TYPE_NAME_Must_Not_Null = "الإسم لا يمكن اي يكون فارغا";
        public const string Type_Max_Length = "لا يمكن أن يزيد اسم النوع عن 250 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string QuantityTypeIdRequired = "يجب إختيار النوع الذي سيتم تعديله";
    }
    #endregion
    #region CircleValidationResponseConstants 
    public static class CircleValidationResponseConstants
    {
        public const string NAME_Must_Not_Null = "الإسم لا يمكن اي يكون فارغا";
        public const string Name_Max_Length = "لا يمكن أن يزيد اسم الحلقة عن 250 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string DaysRequired = "يجب اختيار الأيام";
        public const string DaysMustBeMoreThanZero = "يجب اختيار رقم اكبر من صفر في الأيام";
        public const string DayRequired = "اليوم مطلوب";
        public const string StartTimeRequired = "وقت البداية مطلوب";
        public const string StartTimeInvalid = "وقت البداية غير صحيح";

    }
    #endregion
    #region CircleReportValidationResponseConstants 
    public static class CircleReportValidationResponseConstants
    {
     
        public const string Surah_Must_Not_Null = "اسم السورة لا يمكن اي يكون فارغا";
        public const string Teacher_Must_Not_Null = "اسم المعلم لا يمكن اي يكون فارغا";
        public const string Student_Must_Not_Null = "اسم الطالب لا يمكن اي يكون فارغا";
        public const string Circle_Must_Not_Null = "اسم الحلقة لا يمكن اي يكون فارغا";
        public const string Minutes_Must_Not_Null = " الدقائق لا يمكن اي تكون فارغة";


    }
    #endregion
    #region NeighborhoodValidationResponseConstants 
    public static class NeighborhoodValidationResponseConstants
    {
        public const string NAME_Must_Not_Null = "الإسم لا يمكن اي يكون فارغا";
        public const string Name_Max_Length = "لا يمكن أن يزيد اسم الحي عن 250 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string NeighborhoodIdRequired = "يجب إختيار الحي الذي سيتم تعديله";
    }
    #endregion
    #region RoadValidationResponseConstants 
    public static class RoadValidationResponseConstants
    {
        public const string NAME_Must_Not_Null = "الإسم لا يمكن اي يكون فارغا";
        public const string Name_Max_Length = "لا يمكن أن يزيد اسم الشارع عن 250 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string RoadIdRequired = "يجب إختيار الحي الذي سيتم تعديله";
    }
    #endregion
    #region MainCategoryValidationResponseConstants 
    public static class MainCategoryValidationResponseConstants
    {
        public const string NameNotNullOrEmpty = "الإسم لا يمكن اي يكون فارغا";
        public const string Name_Max_Length = "لا يمكن أن يزيد اسم التصنيف عن 250 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
    }
    #endregion
    #region SubCategoryValidationResponseConstants 
    public static class SubCategoryValidationResponseConstants
    {
        public const string NameNotNullOrEmpty = "الإسم لا يمكن اي يكون فارغا";
        public const string Name_Max_Length = "لا يمكن أن يزيد اسم التصنيف الفرعي عن 250 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string MainCategoryRequired = "التصنيف الرئيسي مطلوب";
        public const string QuantityTypeRequired = "وحدة القياس مطلوبة";
        public const string ValidCost = "التكلفة المتوقعة يجب أن تكون أكبر من أو تساوي صفر في حالة وضع التكلفة المتوقعة";
    }
    #endregion
    #region PrioretyValidationResponseConstants 
    public static class PrioretyValidationResponseConstants
    {
        public const string NameNotNullOrEmpty = "الإسم لا يمكن اي يكون فارغا";
        public const string Name_Max_Length = "لا يمكن أن يزيد اسم درجة الخطورة عن 250 حرف !";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
    }
    #endregion
    #region MobadaraValidationResponseConstants 
    public static class MobadaraValidationResponseConstants
    {
        public const string NameNotNullOrEmpty = "الإسم لا يمكن اي يكون فارغا";
        public const string Name_Max_Length = "لا يمكن أن يزيد اسم المبادرة عن 250 حرف !";
        public const string StartDateRequired = "تاريخ البداية مطلوب";
        public const string StartDateSmallerThanEndDate = "تاريخ البداية يجب أن يكون أصغر من تاريخ النهاية";
        public const string EndDateRequired = "تاريخ النهاية مطلوب";
        public const string EndDateGreaterThanStartDate = "تاريخ النهاية يجب أن يكون أكبر من تاريخ البداية";
        public const string ValidName = "لا يجب وضع علامات خاصة في الإسم";
        public const string MobadaraCentersRequired = "يجب اختيار بلدية علي الأقل";
    }
    #endregion
    #region IncidentValidationReponseConstants
    public static class IncidentValidationReponseConstants
    {
       
        public const string QuantityValueRequired = "يجب وضع قيمة";
        public const string QuantityTypeRequired = "يجب إختيار نوع القيمة";
        public const string CategoryRequired = "يجب إختيار التصنيف";
        public const string CenterRequired = "يجب إختيار البلدية";
        public const string DepartmentRequired = "يجب إختيار الادارة";
        public const string PrioretyLevelRequired = "يجب إختيار درجه الخطورة";
        public const string FilesRequired = "يجب رفع ملف";
        public const string LongAndLangRequired = "يجب اختيار موقع";
        public const string NotesRequired = "الملاحظات مطلوبة";
    }
    #endregion
    #region TempIncidentValidationReponseConstants
    public static class TempIncidentValidationReponseConstants
    {
        public const string ReferenceIdRequired = "الرقم المرجعى مطلوب";
        public const string ReportTimeRequired = "وقت الرصد مطلوب";
        public const string LongitudeRequired = "خط الطول مطلوب";
        public const string LatitudeRequired = "خط العرض مطلوب";
        public const string StatusIdRequired = "كود الحالة مطلوب";
        public const string FileNameRequired = "اسم الملف مطلوب";
        public const string FilePathRequired = "مسار مطلوب";
    }
    #endregion
    #region PhotoScanningValidationResponseConstants
    public static class PhotoScanningValidationResponseConstants
    {
        public const string IdRequired = "الرقم التعريفى مطلوب";
        public const string NameRequired = "الإسم مطلوب";
        public const string ReportTimeRequired = "وقت الرصد مطلوب";
    }
    #endregion

    #region BLL Responses MessageCodes
    public enum MessageCodes

    {

        [Description("Success")]
        Success = 1000,

        [Description("Internal Server Error")]
        Failed = 2000,

        [Description("Failed To Fetch Data")]
        FailedToFetchData = 2001,

        [Description("There is NoPermission to Perform this Action")]
        UnAuthorizedAccess = 4000,

        //Exception
        [Description("Some Error Occurred")]
        Exception = 5000,

        //InputValidation
        [Description("Input Validation Error")]
        InputValidationError = 6000,

        [Description("{0} Is Required")]
        Required = 6001,

        [Description("Failed: {0}  Must Be Greater Than Zero")]
        GreaterThanZero = 6002,

        [Description("Length Validation Error")]
        LengthValidationError = 6003,

        [Description("{2} Must Be Between {0} And {1}")]
        InbetweenValue = 6004,

        [Description("{1} Must Be GreaterThan {0}")]
        InvalidMinLength = 6005,

        [Description("{1} Must Be LessThan {0}")]
        InvalidMaxLength = 6006,

        [Description("Invalid Email")]
        InvalidEmail = 6007,

        [Description("Failed :Invalid Items Count")]
        InvalidItemsCount = 6008,

        [Description("Failed :Invalid Logo")]
        InvalidLogo = 6009,

        [Description("Failed :Invalid Json")]
        InvalidJson = 6010,

        [Description("Failed :Invalid Json Empty Value")]
        InvalidJsonEmptyValue = 6011,

        [Description("Failed :Failed To Deserialize")]
        FailedToDeserialize = 6012,

        [Description("Failed :Missing Default Value")]
        MissingDefaultValue = 6013,

        [Description("Failed :Missing Arabic Value")]
        MissingArabicValue = 6014,

        [Description("Failed :Password should contain at least 1 digit")]
        MissingPasswordDigits = 6015,

        [Description("Failed :Password should contain at least one alphabetic character")]
        MissingPasswordAlphabetic = 6016,

        [Description("Failed :Password should contain at least one special characters Like { ., $, ~ ,&}")]
        MissingPasswordSpecialCharacters = 6017,

        [Description("Failed :Invalid Https Url")]
        InvalidHttpsUrl = 6018,

        [Description("Failed :Invalid File Type")]
        InvalidFileType = 6019,

        [Description("Failed :Invalid File Content Type")]
        InvalidFileContentType = 6020,

        [Description("Failed :Invalid File Size,, Must be less than 2 MB")]
        InvalidFileSize = 6021,

        [Description("Failed :Invalid Rate, Must be within 1 to 5")]
        InvalidRate = 6022,

        [Description("Failed :You Must select one at least")]
        InvalidItemsSelect = 6023,

        //Business Validation
        [Description("Business Validation Error")]
        BusinessValidationError = 7000,

        [Description("{0} Already Exists")]
        AlreadyExists = 7001,

        [Description("{0} Not Found")]
        NotFound = 7002,
       

        [Description("{0} Is DefaultForOther")]
        DefaultForOther = 7003,

        [Description("There're related data to this item")]
        RelatedDataExist = 7004,

        [Description("File type Is Not supported")]
        FileTypeNotSupported = 7005,

        [Description("لا يمكن تكرار الإسم")]
        NameAlreadyExists = 7006,

       

        [Description("اسم المستخدم موجود من قبل")]
        UserNameAlreadyExists = 7007,

        [Description("البريد الاليكترونى موجود من قبل")]
        EmailAlreadyExists = 7008,

        [Description("Can't Delete Admin User ")]
        CanNotDeleteAdminUser = 7009,

        [Description("{0}  Already Exists")]
        AlreadyExistsEn = 7010,

        [Description("{0}  موجود من قبل")]
        AlreadyExistsAr = 7011,

        //InputValidation
        [Description("error on row number {0}")]
        FailedToImport = 7012,
        [Description("البريد الإلكترونى او كلمة المرور خطأ حاول مرة أخري")]
        FailedToLogin = 7013,
        [Description("الكود المدخل لا يطابق الكود الذي تم إرساله")]
        InvalidCode = 7014,
        [Description("الحساب موقوف")]
        NotActive = 7015,
        [Description("لا توجد بلدية لهذا المستخدم وهذا الموقع")]
        NoCenterForThisLocationAndUser = 7016,
        [Description("لا يوجد حي لهذا الموقع")]
        NoNeighborhoodForThisLocation = 7017,
        [Description("لا يوجد شارع لهذا الموقع")]
        NoRoadForThisLocation = 7018,
        [Description("يجب رفع صورة واحدة علي الأقل")]
        NoImagesUploaded = 7019,
        [Description("لا يوجد بلدية تابعة لك بهذا الموقع")]
        NoCenterForThisLocation = 7020,
        [Description("لا يوجد مشرف او رئيس إدارة أو رئيس بلدية ليتم تكليف هذا البلاغ إليه")]
        NoUsersWithTypeMoshrefOrCenterBossOrDepartmentBossToAssign = 7021,
        [Description("لم يتم تحديد تصنيف البلاغ")]
        NoDetectedCategoryForThisIncident = 7022,
        [Description("لا يمكن تعديل الحالة إلي جديد لأن الحالة الأن ليست تحت المراجعة")]
        FailedToUpdateStatusToNewWhenStatusNotWaitingForReview = 7023,
        [Description("لا يمكن التكليف لأن حالة البلاغ ليست جديد أو مخصص أو معاد الفتح")]
        FailedToAssignWhenStatusNotNewOrAssignedOrReopened = 7024,
        [Description("لا يمكن المعالجة المبدئية للبلاغ لأن حالة البلاغ ليست مخصص أو معاد الفتح")]
        FailedToSolveInitiallyWhenStatusNotAssignedOrReopened = 7025,
        [Description("لا يمكن رفض المعالجة للبلاغ لأن حالة البلاغ ليست تمت المعالجة المبدئية")]
        FailedToRejectSolveInitiallyWhenStatusNotSolvedInitially = 7026,
        [Description("لا يمكن تأكيد المعالجة للبلاغ لأن حالة البلاغ ليست تمت المعالجة المبدئية")]
        FailedToSolveIncidentWhenStatusNotSolvedInitially = 7027,
        [Description("لا يمكن إلغاء البلاغ لأن حالة البلاغ ليست تحت المراجعة")]
        FailedToCancelIncidentWhenStatusNotWaitingForReview = 7028,
        [Description("لا يمكن تعديل البلاغ لأن حالة البلاغ ليست تحت المراجعة")]
        FailedToUpdateIncidentWhenStatusNotWaitingForReview = 7029,
        [Description("لا يمكن تكليف هذا المستخدم لانه ليس تابع لنفس الإدارة أو البلدية لهذا البلاغ")]
        ThisUserNotFollowTheSameDepartmentOrCenter = 7030,
        [Description("يوجد  طلاب مرتبطة بهذا الاشتراك")]
        FailedToRemoveSubscribe= 7031,
        [Description("يوجد  باقات مرتبطة بهذا النوع")]
        FailedToRemoveSubscribeType = 7032,
        [Description("لا يمكن اضافه ا لهذا التصنيف")]
        CanNotAddSubcategorytanotherDepartment =7033,
        [Description("هذا العنصر لدية ارتباطات ولا يمكن حذفه")]
        HasRelation = 7034,
        [Description("رقم الهوية موجود بالفعل")]
        NationalIdAlreadyExisted = 7035,
        [Description("Failed :رئيس الإدارة موجود بالفعل")]
        HeadOfDepatmentAlreadyExists = 7036,
        [Description("Failed :رئيس البلدية موجود بالفعل")]
        HeadOfcenterAlreadyExists = 7037,
        [Description("رقم الهاتف موجود بالفعل")]
        PhoneNumberAlreadyExisted = 7038,
        [Description("الرقم السري مطلوب")]
        PasswordIsRequired = 7039,
        [Description("حسابك لم يتم تفعيله بعد. يرجى التحقق من بريدك الإلكتروني وإنشاء كلمة مرور لتتمكن من تسجيل الدخول")]
        PasswordIsNullInThisUser = 7040,
        [Description("تم تفعيل الحساب من قبل ولذلك يجب علي مدير النظام أن يفعل هذا الحساب")]
        CannotActiveThisAccountByEmail = 7041,
        [Description("سجل الدخول أولا")]
        NotLoggedIn = 7042,
        [Description("هذا الحساب ليس مصرح له بتسجيل الدخول")]
        CannotLoginFromMobile = 7043,
        [Description("لا يمكن رفض المراجعة للبلاغ لأن حالة البلاغ ليست تمت المراجعة")]
        FailedToRejectReviewWhenStatusNotReviewed = 7044,
        [Description("لا يمكن الحصول علي معلومات هذا البلاغ لأنه ليس تابع لبلدياتك أو إداراتك")]
        FailedToGetIncidentData = 7045,
        [Description("لا يمكن الحصول علي معلومات هذا المستخدم لأنه ليس تابع لبلدياتك أو إداراتك")]
        FailedToGetUserData = 7046,
        [Description("لا يمكن الحصول علي معلومات هذه المبادرة لأنها ليست تابعة لبلدياتك")]
        FailedToGetMobadaraData = 7047,
        [Description("لا يمكن المعالجة المبدئية للبلاغ لأنك لست الشخص المكلف بالمعالجة")]
        FailedToSolveInitially = 7048,
        [Description("لا يمكن تكليف هذا الشخص لأنه المكلف علي هذا البلاغ بالفعل")]
        CannotAssignThisUserAgain = 7049,
        [Description("انت لست الشخص المكلف بهذا البلاغ")]
        FailedToGetIncidentForCurrentResolver = 7050,
        [Description("لا يمكن رفض المعالجة للبلاغ لأنك لست الشخص المسؤول عن البلاغ")]
        FailedToRejectSolveInitiallyForThisUser = 7051,
        [Description("لا يمكن رفض التخصيص")]
        FailedToRejectAssignIncident = 7052,
        [Description("انت لست الشخص المخصص لهذا البلاغ لترفض التخصيص")]
        FailedToRejectAssignIncidentByLoggedInUser = 7053,
        [Description("لا يمكن إرجاع البلاغ مهذا المستخدم")]
        FailedToReOpenIncidentForThisUser = 7054,
        [Description("الرقم المرجعى الخاص بالرصد الذكى غير موجود")]
        TempIncidentIdNotFound = 7055,
        [Description("المشرف ليس له الحق فى مراجعة البلاغ")]
        IncidentNotAssignedOnSupervisor = 7056,
        [Description("لايمكن الحذف في الوقت الحالي")]
        CannotDeleteNow = 7057, 
        [Description("لقد وصلت الحد الاقصي لعدد مرات المحاولة، انتظر قليلا وحاول مرة اخري")]
        ReachedForFailedAttempts = 7058,
        [Description("عفواً لم يتم ارسال بريد الكترونى، حاول مرة اخري")]
        CouldnotSendEmail = 7059,
        [Description("البلاغ تم قبوله او رفضه من قبل")]
        TempIncidentIsnotNew = 7060,
        [Description("لم يتمكن من تغيير حالة بلاغ الرصد الذكى، برجاء المحاولة مجدداً")]
        CouldnotChangeTempIncidentStatus = 7061,
        [Description("لقد انتهت صلاحية رمز التحقق أو تجاوزت الحد الأقصى للمحاولات. يرجى إعادة محاولة تسجيل الدخول")]
        CodeNotValidAnyMore = 7062,
        [Description("يجب تحديد النطاق الجغرافي")]
        MustSendCoordinates = 7063,
        [Description("لا يمكن تكرار التقرير لنفس الطالب في نفس اليوم")]
        ReportAlreadyExists = 7064,
        [Description("لا يوجد طالب بهذا الاسم")]
        StudentNotFound = 7065,
        [Description("لا يوجد معلم بهذا الاسم")]
        TeacherNotFound = 7066,
        [Description("لا يوجد باقة لهذا الطالب")]
        StudentSubscribeNotFound = 7067,
        [Description("لا يوجد دقائق كافية لهذا الطالب")]
        StudentMinutesNotFound = 7068,
        [Description("كلمة المرور الحالية غير صحيحة")]
        InvalidCurrentPassword = 7069,
    }

    #endregion BLL Responses MessageCodes

    public static class CacheKeys
    {
        public const string ActiveSupervisor = "ACTIVE_SUPERVISOR";
        public const string SupervisorIndex = "SUPERVISOR_INDEX";
        public const string IncidentSupervisorIndex = "INCIDENT_SUPERVISOR_INDEX";
        public const string IncidentsCategories = "INCI_CATEGORIES";
        public const string IncidentsListCacheKeys = "INCI_CACHE_KEYS";
        public const string IncidentsListCountCacheKeys = "INCI_COUNT_KEYS";
    }
}