using System;

namespace Orbits.GeneralProject.DTO.ManagerDto
{
    public class ManagerDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public int? UserTypeId { get; set; }
        public int? NationalityId { get; set; }
        public int? ResidentId { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
    }
}