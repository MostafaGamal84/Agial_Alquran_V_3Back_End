using FluentValidation;
using Microsoft.AspNetCore.Http;
using Orbits.GeneralProject.BLL.StaticEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Validation
{
    public class FileValidation : AbstractValidator<IFormFile>
    {
        public FileValidation(long size, Type enumType)
        {
            RuleFor(x => x.Length).Must(xx => FormatSize(xx) <= size).WithMessage("حجم الملف كبير");
            RuleFor(x => x).Must(xx => CheckFileType(xx, enumType)).WithMessage("لا يمكن رفع هذه الملفات");
        }
        public double FormatSize(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
        public bool CheckFileType(IFormFile file, Type enumType)
        {
            var fileExtension = file.FileName.Split('.').LastOrDefault().ToUpper();
            if (Enum.IsDefined(enumType, fileExtension))
                return true;
            return false;
        }
    }
    public class FilesValidation : AbstractValidator<List<IFormFile>>
    {
        public  FilesValidation(Type enumType)
        {
            RuleForEach(files => files).SetValidator(new FileValidation(5, enumType));
        }
    }




}
