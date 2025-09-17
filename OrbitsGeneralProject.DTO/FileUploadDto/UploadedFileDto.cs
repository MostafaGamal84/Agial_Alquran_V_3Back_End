using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.FileUploadDto
{
    public class UploadedFileDto
    {
        public int Id { get; set; }
        public string? FilePath { get; set; }
        public string? OriginalName { get; set; }
        public decimal? Size { get; set; }
    }
}
