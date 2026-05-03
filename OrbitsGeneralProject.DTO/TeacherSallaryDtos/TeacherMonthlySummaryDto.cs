using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    public class TeacherMonthlySummaryDto
    {
        public int TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public DateTime Month { get; set; }
        public int TotalReports { get; set; }
        public double TotalMinutes { get; set; }
        public int PresentCount { get; set; }
        public int AbsentWithExcuseCount { get; set; }
        public int AbsentWithoutExcuseCount { get; set; }
        public double TotalSalary { get; set; }
        public List<TeacherSalarySectionBreakdownDto> SectionBreakdown { get; set; } = new();
        public TeacherInvoiceDto? Invoice { get; set; }
    }
}
