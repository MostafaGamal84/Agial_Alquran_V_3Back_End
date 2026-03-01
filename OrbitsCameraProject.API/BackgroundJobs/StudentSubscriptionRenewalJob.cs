using System.Net.Http.Json;
using Hangfire;
using Orbits.GeneralProject.Core.Entities;
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
        private const string DefaultRenewalEndpoint = "api/StudentSubscrib/Create";

        private readonly IRepository<StudentSubscribe> _studentSubscribeRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StudentSubscriptionRenewalJob> _logger;

        public StudentSubscriptionRenewalJob(
            IRepository<StudentSubscribe> studentSubscribeRepo,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<StudentSubscriptionRenewalJob> logger)
        {
            _studentSubscribeRepo = studentSubscribeRepo;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task RenewSubscriptionsAsync()
        {
            var apiBaseUrl = _configuration["HangfireJobs:ApiBaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                _logger.LogWarning("Monthly student subscription renewal skipped because HangfireJobs:ApiBaseUrl is not configured.");
                return;
            }

            var renewalEndpoint = _configuration["HangfireJobs:StudentSubscriptionRenewalEndpoint"] ?? DefaultRenewalEndpoint;

            var latestStudentSubscriptions = _studentSubscribeRepo
                .GetAll()
                .Where(x => x.StudentId.HasValue && x.StudentSubscribeId.HasValue)
                .GroupBy(x => x.StudentId!.Value)
                .Select(g => g
                    .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new AddStudentSubscribeDto
                    {
                        StudentId = x.StudentId,
                        StudentSubscribeId = x.StudentSubscribeId
                    })
                    .First())
                .ToList();

            if (latestStudentSubscriptions.Count == 0)
            {
                _logger.LogInformation("Monthly student subscription renewal found no students to renew.");
                return;
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiBaseUrl);

            foreach (var renewalRequest in latestStudentSubscriptions)
            {
                try
                {
                    var response = await client.PostAsJsonAsync(renewalEndpoint, renewalRequest);
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning(
                            "Failed to renew subscription for StudentId {StudentId}. Status: {StatusCode}. Body: {ResponseBody}",
                            renewalRequest.StudentId,
                            response.StatusCode,
                            body);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to renew subscription for StudentId {StudentId} with StudentSubscribeId {StudentSubscribeId}",
                        renewalRequest.StudentId,
                        renewalRequest.StudentSubscribeId);
                }
            }

            _logger.LogInformation(
                "Monthly student subscription renewal job completed for {Count} students.",
                latestStudentSubscriptions.Count);
        }
    }
}
