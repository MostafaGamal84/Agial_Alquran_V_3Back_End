using Orbits.GeneralProject.BLL.StudentSubscribeService;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.Repositroy.Base;

namespace OrbitsProject.API.BackgroundJobs
{
    public interface IStudentSubscriptionRenewalJob
    {
        Task RenewSubscriptionsAsync();
    }

    public class StudentSubscriptionRenewalJob : IStudentSubscriptionRenewalJob
    {
        private readonly IRepository<StudentSubscribe> _studentSubscribeRepo;
        private readonly IStudentSubscribeBLL _studentSubscribeBll;
        private readonly ILogger<StudentSubscriptionRenewalJob> _logger;

        public StudentSubscriptionRenewalJob(
            IRepository<StudentSubscribe> studentSubscribeRepo,
            IStudentSubscribeBLL studentSubscribeBll,
            ILogger<StudentSubscriptionRenewalJob> logger)
        {
            _studentSubscribeRepo = studentSubscribeRepo;
            _studentSubscribeBll = studentSubscribeBll;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        public async Task RenewSubscriptionsAsync()
        {
            var (currentMonthStartUtc, currentMonthEndUtc) = BusinessDateTime.GetCurrentCairoMonthRangeUtc();

            var activeSubscriptionsQuery = _studentSubscribeRepo
                .GetAll()
                .AsNoTracking()
                .Where(x =>
                    x.StudentId.HasValue &&
                    x.StudentSubscribeId.HasValue &&
                    x.Student != null &&
                    x.Student.IsDeleted == false);

            var renewedStudentIdsThisMonth = activeSubscriptionsQuery
                .Where(x =>
                    x.CreatedAt.HasValue &&
                    x.CreatedAt.Value >= currentMonthStartUtc &&
                    x.CreatedAt.Value < currentMonthEndUtc)
                .Select(x => x.StudentId!.Value)
                .Distinct();

            var latestStudentSubscriptions = activeSubscriptionsQuery
                .Where(x => !renewedStudentIdsThisMonth.Contains(x.StudentId!.Value))
                .GroupBy(x => x.StudentId!.Value)
                .Select(g => g
                    .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new AddStudentSubscribeDto
                    {
                        StudentId = x.StudentId,
                        StudentSubscribeId = x.StudentSubscribeId,
                        ActionType = "MonthlyRenewal"
                    })
                    .First())
                .ToList();

            if (latestStudentSubscriptions.Count == 0)
            {
                _logger.LogInformation("Monthly student subscription renewal found no students to renew.");
                return;
            }

            int renewedCount = 0;
            int failedCount = 0;

            foreach (var renewalRequest in latestStudentSubscriptions)
            {
                try
                {
                    var result = await _studentSubscribeBll.AddAsync(renewalRequest, userId: null);
                    if (!result.IsSuccess)
                    {
                        failedCount++;
                        var errorMessage = result.Errors != null && result.Errors.Count > 0
                            ? string.Join(" | ", result.Errors.Select(error => error.Message))
                            : "Unknown renewal failure.";
                        _logger.LogWarning(
                            "Failed to renew subscription for StudentId {StudentId} with StudentSubscribeId {StudentSubscribeId}. Errors: {Errors}",
                            renewalRequest.StudentId,
                            renewalRequest.StudentSubscribeId,
                            errorMessage);
                        continue;
                    }

                    renewedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex,
                        "Failed to renew subscription for StudentId {StudentId} with StudentSubscribeId {StudentSubscribeId}",
                        renewalRequest.StudentId,
                        renewalRequest.StudentSubscribeId);
                }
            }

            _logger.LogInformation(
                "Monthly student subscription renewal job completed. Total: {TotalCount}, Renewed: {RenewedCount}, Failed: {FailedCount}.",
                latestStudentSubscriptions.Count,
                renewedCount,
                failedCount);
        }
    }
}
