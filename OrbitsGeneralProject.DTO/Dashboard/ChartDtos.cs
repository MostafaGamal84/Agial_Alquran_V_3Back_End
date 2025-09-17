using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.Dashboard
{
    public class ChartDto
    {
        public List<string> Categories { get; set; } = new();
        public List<ChartSeriesDto> Series { get; set; } = new();
    }

    public class ChartSeriesDto
    {
        public string Name { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
    }

    public class PieChartDto
    {
        public List<PieChartSliceDto> Slices { get; set; } = new();
        public decimal TotalValue { get; set; }
    }

    public class PieChartSliceDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Percentage { get; set; }
    }
}
