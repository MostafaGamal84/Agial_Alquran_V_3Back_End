using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.AcademicCircleDtos
{
    public class CreateAcademicCircleDto
    {
        public string? Name { get; set; }
        public int? TeacherId { get; set; }
        public List<int>? ManagerIds { get; set; } = new();
        public List<int>? StudentIds { get; set; } = new();
        public int? BranchId { get; set; }
    }
}
