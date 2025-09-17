using Microsoft.AspNetCore.Http;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.FileUploadDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.FilesUploaderService
{
    public interface IFileServiceBLL
    {


        Task<IResponse<FileUploadReturnDto>> CreateFileAsync(IFormFile file, string? path);
   
    }

}
