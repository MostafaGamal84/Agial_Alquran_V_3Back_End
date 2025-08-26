using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UpdateAccountDto
    {
        public string Username { get; set; }
        public string Mobile { get; set; }
        public string? Password { get; set; } 
        public string? JobName { get; set; } 
        public string? NationalId { get; set; } 
        public DateTime? BirthDate { get; set; } 
        
    }
}
