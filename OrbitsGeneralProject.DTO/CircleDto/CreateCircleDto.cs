using Orbits.GeneralProject.Core.Entities;
using System;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class CreateCircleDto
    {
       
     
        public string? Name { get; set; }
        public int? TeacherId { get; set; }

        public List<int>? Managers { get; set; }

        public List<int>? StudentsIds { get; set; } = new List<int>();


    }
}