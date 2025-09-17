using Microsoft.AspNetCore.Http;
using Orbits.GeneralProject.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.StudentPaymentDtos
{
    public class UpdatePaymentDto
    {
        public int Id { get; set; }
        public int? Amount { get; set; }
        public IFormFile? ReceiptPath { get; set; }
        public bool? PayStatue { get; set; }
        public bool? IsCancelled { get; set; }
    }
}
