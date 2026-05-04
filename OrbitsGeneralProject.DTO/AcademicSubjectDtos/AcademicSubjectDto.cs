using System;

namespace Orbits.GeneralProject.DTO.AcademicSubjectDtos
{
    public class AcademicSubjectDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? DisplayOrder { get; set; }
        public int ReportsCount { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
