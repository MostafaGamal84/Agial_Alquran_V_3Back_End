using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.AcademicCircleDtos
{
    public class AcademicCircleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int? BranchId { get; set; }
        public List<int> ManagerIds { get; set; } = new();
        public List<string> ManagerNames { get; set; } = new();
        public List<int> StudentIds { get; set; } = new();
        public List<string> StudentNames { get; set; } = new();
        public int StudentCount { get; set; }
    }
}
