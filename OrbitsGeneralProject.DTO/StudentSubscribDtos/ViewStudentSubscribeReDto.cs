using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.StudentSubscribDtos
{
    public class ViewStudentSubscribeReDto
    {
        public int? Id { get; set; }
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentMobile { get; set; }
        public bool? PayStatus { get; set; }
        public string? Plan { get; set; }
        public int? RemainingMinutes { get; set; }
        public DateTime? StartDate { get; set; }
        public int? StudentPaymentId { get; set; }





    }
}
