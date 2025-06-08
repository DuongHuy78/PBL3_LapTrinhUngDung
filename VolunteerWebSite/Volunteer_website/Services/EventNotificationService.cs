using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volunteer_website.Models;

namespace Volunteer_website.Services
{
    public class EventNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventNotificationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Kiểm tra mỗi 30 giây để test
        private readonly string _fromEmail = "volunteer.web.pbl3@gmail.com";
        private readonly string _toEmail = "quocdat19991712@gmail.com";

        public EventNotificationService(IServiceProvider serviceProvider, ILogger<EventNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Bắt đầu kiểm tra sự kiện sắp tới: {Time}", DateTime.Now);
                await SendEventNotifications();
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task SendEventNotifications()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<VolunteerManagementContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                // Truy vấn các sự kiện sắp tới (DayBegin trong vòng 24 giờ tới)
                var today = DateOnly.FromDateTime(DateTime.Today);
                var tomorrow = today.AddDays(1);
                var upcomingEvents = context.Registrations
                    .Include(r => r.Volunteer)
                    .Include(r => r.Event)
                    .Where(r => r.Event != null && r.Event.DayBegin.HasValue && r.Event.DayBegin >= today && r.Event.DayBegin <= tomorrow)
                    .ToList();

                _logger.LogInformation("Số sự kiện sắp tới tìm thấy: {Count}", upcomingEvents.Count);

                foreach (var registration in upcomingEvents)
                {
                    if (registration.Volunteer?.Name == null || registration.Event == null)
                    {
                        _logger.LogWarning("Bỏ qua registration: Volunteer hoặc Event null, RegId: {RegId}", registration.RegId);
                        continue;
                    }

                    try
                    {
                        var subject = $"Nhắc nhở: Sự kiện sắp tới - {registration.Event.Name}";
                        var body = $@"Kính gửi {registration.Volunteer.Name},

Bạn đã đăng ký tham gia sự kiện '{registration.Event.Name}' diễn ra vào {registration.Event.DayBegin.Value:dd/MM/yyyy}.
Địa điểm: {registration.Event.Location ?? "N/A"}
Vui lòng chuẩn bị kỹ lưỡng. Nếu có câu hỏi, liên hệ với chúng tôi qua {_fromEmail}.

Trân trọng,
Đội ngũ Volunteer Website";

                        _logger.LogInformation("Gửi email cho sự kiện: {EventName}, đến: {ToEmail}", registration.Event.Name, _toEmail);
                        await emailService.SendEmailAsync(_toEmail, subject, body);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gửi email thất bại đến {ToEmail} cho sự kiện {EventName}", _toEmail, registration.Event.Name);
                    }
                }
            }
        }
    }
}