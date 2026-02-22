using Orbits.GeneralProject.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.CircleReportDtos
{
    public class CircleReportAddDto
    {
        public int? Id { get; set; }
        public double? Minutes { get; set; }
        public int? NewId { get; set; }
        public string? NewFrom { get; set; }
        public string? NewTo { get; set; }
        public string? RecentPast { get; set; }
        public string? DistantPast { get; set; }
        public string? FarthestPast { get; set; }
        public string? GeneralRate { get; set; }
        public string? TheWordsQuranStranger { get; set; }
        public string? Intonation { get; set; }
        public string? Other { get; set; }
        public DateTime CreationTime { get; set; }
        public int? CircleId { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? AttendStatueId { get; set; }
        public bool? IsVisual { get; set; }
        public string? NextCircleOrder { get; set; }
      
       
    }
}
