namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    public class TeacherSalarySectionBreakdownDto
    {
        public string SectionName { get; set; } = string.Empty;
        public double TotalMinutes { get; set; }
        public double TotalHours { get; set; }
        public double TotalSalary { get; set; }
    }
}
