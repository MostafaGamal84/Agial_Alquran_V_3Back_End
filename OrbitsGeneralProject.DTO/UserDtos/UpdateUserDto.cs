using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Mobile { get; set; }
        public string? JobId { get; set; }
        public string? JobName { get; set; }
        public string? NationalId { get; set; }
        public int? UserTypeId { get; set; }
        public List<int?> CenterIds { get; set; } = new List<int?>();
        public List<int?> DepartmentIds { get; set; } = new List<int?>();
    }
}
