using System;

namespace Orbits.GeneralProject.DTO.UserDto
{
    public class UserReturnDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public string? PasswordHash { get; set; }
        public int? UserTypeId { get; set; }
        public int? NationalityId { get; set; }
        public int? ResidentId { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
        public int? ManagerId { get; set; }
        public int? TeacherId { get; set; }
        public int? CircleId { get; set; }

    }
}