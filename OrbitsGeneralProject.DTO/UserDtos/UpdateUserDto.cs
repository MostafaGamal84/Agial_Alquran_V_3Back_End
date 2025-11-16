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
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public int? NationalityId { get; set; }
        public int? ResidentId { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
        public int? ManagerId { get; set; }
        public int? TeacherId { get; set; }
        public int? CircleId { get; set; }
        public List<int>? TeacherIds { get; set; }
        public List<int>? CircleIds { get; set; }

        
        public List<int>? StudentIds { get; set; } = new List<int>();
    }
}
