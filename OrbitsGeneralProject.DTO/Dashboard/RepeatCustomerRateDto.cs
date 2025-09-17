namespace Orbits.GeneralProject.DTO.Dashboard
{
    public class RepeatCustomerRateDto
    {
        public ChartDto Chart { get; set; } = new();
        public decimal CurrentRate { get; set; }
        public decimal PreviousRate { get; set; }
        public decimal RateChange { get; set; }
    }
}
