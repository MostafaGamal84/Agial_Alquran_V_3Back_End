using AutoMapper;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.CircleReportDtos;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.RegionDtos;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.DTO.StudentSubscribDtos.StudentPaymentDtos;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orbits.GeneralProject.BLL.Mapping
{
    public class EntityToDtoMappingProfile : Profile
    {
        public EntityToDtoMappingProfile()
        {
            CreateMap<User, ManagerDto>()
            .ForMember(x => x.UserTypeId, xx => xx.MapFrom(c => ((UserTypesEnum)c.UserTypeId).ToString()));
            CreateMap<Circle, CircleDto>()
                .ForMember(d => d.Managers, o => o.MapFrom(s => s.ManagerCircles))
                .ForMember(d => d.Students, o => o.MapFrom(s => s.Users.Where(X => X.UserTypeId == (int)UserTypesEnum.Student)))
                .ForMember(d => d.Days, o => o.MapFrom(s => s.CircleDays));
            CreateMap<CircleDay, CircleDayDto>()
                .ForMember(d => d.DayId, o => o.MapFrom(s => s.DayId.HasValue ? s.DayId.Value : 0))
                .ForMember(d => d.Time, o => o.MapFrom(s => s.Time))
                .ForMember(d => d.DayName, o => o.MapFrom(s => (s.DayId.HasValue && Enum.IsDefined(typeof(DaysEnum), s.DayId.Value)
                            ? ((DaysEnum)s.DayId.Value).ToString()
                            : null)));
            CreateMap<User, UserReturnDto>();
            CreateMap<User, ManagerDto>();
            CreateMap<User, ProfileDto>();
            CreateMap<User, UserLockUpDto>()
                .ForMember(x => x.Nationality, xx => xx.MapFrom(c => c.Nationality.Name))
                .ForMember(x => x.Resident, xx => xx.MapFrom(c => c.Resident.Name))
                .ForMember(x => x.Governorate, xx => xx.MapFrom(c => c.Governorate.Name))
                .ForMember(x => x.CircleName, xx => xx.MapFrom(c => c.Circle.Name))
                .ForMember(x => x.CircleId, xx => xx.MapFrom(c => c.CircleId))
              ;
            CreateMap<User, LookupDto>().ForMember(x => x.Name, xx => xx.MapFrom(c => c.FullName));
            CreateMap<Circle, LookupDto>();



            CreateMap<ManagerCircle, ManagerCirclesDto>()
                                .ForMember(x => x.Manager, xx => xx.MapFrom(c => c.Manager.FullName))
                                .ForMember(x => x.Circle, xx => xx.MapFrom(c => c.Circle.Name))
;
            CreateMap<Nationality, RegionDto>();
            CreateMap<Governorate, RegionDto>();

            CreateMap<Subscribe, SubscribeReDto>()
            .ForMember(x => x.SubscribeType, xx => xx.MapFrom(c => c.SubscribeType))
;
            CreateMap<SubscribeType, SubscribeTypeReDto>()
                .ForMember(d => d.Group, o => o.MapFrom(s => s.Group.HasValue ? (SubscribeTypeCategory?)s.Group.Value : null));


            CreateMap<User, ViewStudentSubscribeReDto>()
                .ForMember(d => d.StudentId, m => m.MapFrom(c => c.Id))
                .ForMember(d => d.StudentName, m => m.MapFrom(c => c.FullName))
                .ForMember(d => d.StudentMobile, m => m.MapFrom(c => c.Mobile))
                // RemainingMinutes of the latest subscription (by CreatedAt)
                .ForMember(d => d.RemainingMinutes, m => m.MapFrom(c => c.StudentSubscribes!.Where(x => x.StudentId == c.Id).LastOrDefault().RemainingMinutes!))
                            .ForMember(d => d.StartDate, m => m.MapFrom(c => c.StudentSubscribes!.Where(x => x.StudentId == c.Id).LastOrDefault().CreatedAt!))
                            .ForMember(d => d.Plan, m => m.MapFrom(c => c.StudentSubscribes!.Where(x => x.StudentId == c.Id).LastOrDefault().StudentSubscribeType.Name!))
                            .ForMember(d => d.PayStatus, m => m.MapFrom(c => c.StudentSubscribes!.Where(x => x.StudentId == c.Id).LastOrDefault().PayStatus!))
                            .ForMember(d => d.StudentPaymentId, m => m.MapFrom(c => c.StudentSubscribes!.Where(x => x.StudentId == c.Id).LastOrDefault().StudentPaymentId!));
            CreateMap<StudentPayment, StudentPaymentReDto>()
     .ForMember(d => d.InvoiceId, m => m.MapFrom(s => s.Id))
     .ForMember(d => d.StudentId, m => m.MapFrom(s => s.StudentId ?? 0))
     .ForMember(d => d.UserName, m => m.MapFrom(s => s.Student != null ? s.Student.FullName : null))
     .ForMember(d => d.UserEmail, m => m.MapFrom(s => s.Student != null ? s.Student.Email : null))
     .ForMember(d => d.Subscribe, m => m.MapFrom(s => s.StudentSubscribe!.Name))
     .ForMember(d => d.CreateDate, m => m.MapFrom(s => s.CreatedAt ?? DateTime.MinValue))
     .ForMember(d => d.PaymentDate, m => m.MapFrom(s => s.PaymentDate ?? null))
     .ForMember(d => d.DueDate,
    m => m.MapFrom(s => new DateTime(
        s.CreatedAt.Value.Year,
        s.CreatedAt.Value.Month,
        1).AddMonths(1)))
     .ForMember(d => d.Amount, m => m.MapFrom(s => (decimal)(s.Amount ?? 0)))
     .ForMember(d => d.StatusText, m => m.MapFrom(s =>
         s.IsCancelled == true ? "Cancelled" :
         s.PayStatue == true ? "Paid" : "Unpaid"));

            CreateMap<StudentSubscribe, ViewStudentSubscribeReDto>()
                .ForMember(d => d.StudentId, m => m.MapFrom(c => c.StudentId))
                .ForMember(d => d.StudentName, m => m.MapFrom(c => c.Student.FullName))
                .ForMember(d => d.StudentMobile, m => m.MapFrom(c => c.Student.Mobile))
                // RemainingMinutes of the latest subscription (by CreatedAt)
                .ForMember(d => d.RemainingMinutes, m => m.MapFrom(c => c.RemainingMinutes!))
                            .ForMember(d => d.StartDate, m => m.MapFrom(c => c.CreatedAt!))
                            .ForMember(d => d.Plan, m => m.MapFrom(c => c.StudentSubscribeType.Name!))
                            .ForMember(d => d.PayStatus, m => m.MapFrom(c => c.PayStatus!))
                            .ForMember(d => d.StudentPaymentId, m => m.MapFrom(c => c.StudentPaymentId!));
            CreateMap<StudentPayment, PaymentsFullDashboardDto>();

            CreateMap<CircleReport, CircleReportReDto>()
               .ForMember(d => d.TeacherName, m => m.MapFrom(c => c.Teacher.FullName!))
               .ForMember(d => d.StudentName, m => m.MapFrom(c => c.Student.FullName!))
               .ForMember(d => d.CircleName, m => m.MapFrom(c => c.Circle.Name!));




        }
    }
}
