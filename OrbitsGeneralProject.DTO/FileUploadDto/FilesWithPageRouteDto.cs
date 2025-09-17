using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.FileUploadDto
{
    public class FilesWithPageRouteDto
    {
        public List<IFormFile> Files { get; set; }
        public string PageRoute { get; set; }
        public int PathEnum { get; set; }

    }
    public class UrlWithPageRouteDto
    {
        public string FileUrl { get; set; }
        public string PageRoute { get; set; }
        public int PathEnum { get; set; }

    }
}
