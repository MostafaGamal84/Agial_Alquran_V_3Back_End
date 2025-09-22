using System;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class UpdateCircleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? Time { get; set; }

        public int? TeacherId { get; set; }

        public List<int>? Managers { get; set; }

        public List<int>? StudentsIds { get; set; } = new List<int>();
    }
}