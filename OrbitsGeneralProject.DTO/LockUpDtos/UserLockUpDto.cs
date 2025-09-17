using Orbits.GeneralProject.DTO.CircleDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.LockUpDtos
{
    public class UserLockUpDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public string? Nationality { get; set; }
        public int? NationalityId { get; set; }
        public string? Governorate { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int? CircleId { get; set; }
        public string? CircleName { get; set; }
        public bool? Inactive { get; set; }


        public ICollection<UserLockUpDto>? Managers { get; set; }
        public ICollection<UserLockUpDto>? Teachers { get; set; }
        public ICollection<UserLockUpDto>? Students { get; set; }
        public ICollection<ManagerCirclesDto>? ManagerCircles { get; set; }


    }
}
