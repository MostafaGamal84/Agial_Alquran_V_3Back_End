using AutoMapper;
using Orbits.GeneralProject.DTO.Setting.Files;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Mapping
{

    public class UrlBaseResolver : IMemberValueResolver<object, object, string, string>
    {
        private readonly FileStorageSetting _fileDataSetting;

        public UrlBaseResolver(IOptions<FileStorageSetting> fileOptions)
        {
            _fileDataSetting = fileOptions.Value;
        }

        public string Resolve(object source, object destination, string sourceMember, string destinationMember, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(sourceMember))
                return null;

            return Path.Combine(_fileDataSetting.FileStorageBaseUrlPath,_fileDataSetting.OrganizationsContainerName, sourceMember).Replace("\\", "/");
        }

    }
}
