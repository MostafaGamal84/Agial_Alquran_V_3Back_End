namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public int? NationalityId { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
    }
}
