using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Validation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.FileUploadDto;
using Orbits.GeneralProject.DTO.Setting.FilesPath;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Orbits.GeneralProject.BLL.Constants.DXConstants;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Orbits.GeneralProject.BLL.FilesUploaderService
{
    public class FileServiceBLL : IFileServiceBLL
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly FilePathSetting _filePathSetting;
        private readonly BaseUrl _baseUrlSetting;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<User> _userRepository;
        public FileServiceBLL(IHostEnvironment hostEnvironment, IOptions<FilePathSetting> filePathSetting, IOptions<BaseUrl> baseUrlSetting, IMapper mapper,  IUnitOfWork unitOfWork,  IRepository<User> userRepository)
        {
            _hostEnvironment = hostEnvironment;
            _filePathSetting = filePathSetting.Value;
            _baseUrlSetting = baseUrlSetting.Value;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
        }
      
        public async Task<IResponse<FileUploadReturnDto>> CreateFileAsync(IFormFile file, string? path)
        {
            Response<FileUploadReturnDto> output = new Response<FileUploadReturnDto>();
            if (file == null) return output.CreateResponse(MessageCodes.NotFound);
            //var ValidationResult = await new FileValidation(5, typeof(FileTypeEnum)).ValidateAsync(file);
            //if (!ValidationResult.IsValid)
            //    return output.AppendErrors(ValidationResult.Errors);
            string? fileName = file.FileName;
            string? extention = "." + fileName.Split('.')[fileName.Split('.').Length - 1];
            string? newFileName = Guid.NewGuid() + extention;
            string root = _hostEnvironment.ContentRootPath;
            string? pathDirectory = root + "/" + path;
            if (!Directory.Exists(pathDirectory))
                Directory.CreateDirectory(pathDirectory);
            string? pathFile = Path.Combine(pathDirectory, newFileName);
            using (var stream = System.IO.File.Create(pathFile))
                await file.CopyToAsync(stream);
            FileUploadReturnDto fileUpload = new FileUploadReturnDto();
            fileUpload.FilePath = path + newFileName;
            return output.CreateResponse(fileUpload);
        }
       
    }
}
