namespace Orbits.GeneralProject.DTO.Dashboard
{
    public class MonthlyRevenueChartDto
    {
        public ChartDto Chart { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalTeacherPayout { get; set; }
        public decimal TotalManagerPayout { get; set; }
        public decimal TotalNetIncome { get; set; }
    }
}
