using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.UserDto;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class CircleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? TeacherId { get; set; }
        public UserLockUpDto? Teacher { get; set; }


        [JsonPropertyName("days")]
        public ICollection<CircleDayDto>? Days { get; set; }

        public ICollection<ManagerCirclesDto>? Managers { get; set; }

        public ICollection<UserReturnDto>? Students { get; set; }
    }
}
