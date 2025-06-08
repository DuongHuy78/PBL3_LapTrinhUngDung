using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.Models;
using Volunteer_website.Areas.Admins.Data;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Authentication;
using System.Buffers;
using Volunteer_website.Helpers;
using Volunteer_website.ViewModels;
namespace Volunteer_website.Areas.Admins.Controllers
{
    [Area("Admin")]
    [Route("Admin/HomeAdmin")]
    public class HomeAdminController : Controller
    {
        private readonly VolunteerManagementContext _db;

        public HomeAdminController(VolunteerManagementContext db)
        {
            _db = db;
        }

        #region Trang chủ
        [Route("")]
        [Route("Index")]
        public IActionResult Index(string? startDate = null, string? endDate = null)
        {
            var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                return RedirectToAction("Login", "Account");
            }
            var currentAdmin = _db.Admins.FirstOrDefault(a => a.AdminId == adminId);
            if (currentAdmin == null)
                return NotFound();
            ViewBag.ImgPath = currentAdmin.ImgPath;

            var events = _db.Events.Count();
            var totalAmount = _db.Donations.AsNoTracking()
                                   .Sum(d => d.Amount);
            var volunteers = _db.Volunteers.Count();

            var currentYear = DateTime.Now.Year;
            var startYear = currentYear - 5;
            int[] eventsByYear = new int[6];

            for (int i = 0; i < 6; i++)
            {
                var year = startYear + i;
                eventsByYear[i] = _db.Events.Count(e => e.DayBegin.HasValue && e.DayBegin.Value.Year == year);
            }

            ViewBag.Events = events;
            ViewBag.Donations = totalAmount;
            ViewBag.Volunteers = volunteers;

            // Xử lý ngày mặc định nếu startDate hoặc endDate là null
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            // Định dạng ngày thành chuỗi "yyyy-MM-dd" để hiển thị trong input
            ViewBag.StartDate = startDateOnly.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDateOnly.ToString("yyyy-MM-dd");

            // Lấy dữ liệu thống kê đăng ký theo thời gian
            var statisticsData = StatisticsRegistrationByTime(startDate, endDate);
            var registrationChartData = statisticsData.Select(x => new
            {
                Label = $"{x.Year}-{x.Month:D2}-{x.Day:D2}",
                Count = x.Count
            }).ToList();

            ViewBag.RegistrationStatistics = new
            {
                Labels = registrationChartData.Select(x => x.Label).ToArray(),
                Data = registrationChartData.Select(x => x.Count).ToArray()
            };

            // Lấy dữ liệu thống kê donation theo thời gian
            var donationStatistics = StatisticsDonationByTime(startDate, endDate);
            var donationChartData = donationStatistics.Select(x => new
            {
                Label = $"{x.Year}-{x.Month:D2}-{x.Day:D2}",
                TotalAmount = x.TotalAmount
            }).ToList();

            ViewBag.DonationStatistics = new
            {
                Labels = donationChartData.Select(x => x.Label).ToArray(),
                Data = donationChartData.Select(x => x.TotalAmount).ToArray()
            };

            // Lấy dữ liệu thống kê đăng ký theo sự kiện
            var EventIsActiveData = StatisticsEventIsActiveByTime(startDate, endDate);
            var EventIsActiveChartData = EventIsActiveData.Select(x => new
            {
                Label = $"{x.Year}-{x.Month:D2}-{x.Day:D2}",
                Count = x.Count
            }).ToList();

            ViewBag.EventIsActiveStatistics = new
            {
                Labels = EventIsActiveChartData.Select(x => x.Label).ToArray(),
                Data = EventIsActiveChartData.Select(x => x.Count).ToArray()
            };

            return View();
        }
        #endregion

        #region đăng ký theo thời gian
        public List<dynamic> StatisticsRegistrationByTime(string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            var data = _db.Registrations
                .Where(r => r.RegisterAt.HasValue)
                .Where(r => r.RegisterAt.Value >= startDateOnly && r.RegisterAt.Value <= endDateOnly)
                .GroupBy(r => r.RegisterAt.Value)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToDictionary(x => x.Date, x => x.Count);

            // Create a list of all dates in the range with counts
            var result = new List<dynamic>();
            for (var date = startDateOnly; date <= endDateOnly; date = date.AddDays(1))
            {
                result.Add(new
                {
                    Year = date.Year,
                    Month = date.Month,
                    Day = date.Day,
                    Count = data.ContainsKey(date) ? data[date] : 0
                });
            }

            return result;
        }
        #endregion

        #region Event tổ chức theo thời gian
        public List<dynamic> StatisticsEventIsActiveByTime(string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            var eventData = _db.Events
                .Where(e => e.DayBegin.HasValue && e.DayEnd.HasValue)
                .Where(e => e.DayBegin.Value <= endDateOnly && e.DayEnd.Value >= startDateOnly)
                .GroupBy(e => e.DayBegin.Value)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToDictionary(x => x.Date, x => x.Count);

            var result = new List<dynamic>();
            for (var date = startDateOnly; date <= endDateOnly; date = date.AddDays(1))
            {
                result.Add(new
                {
                    Year = date.Year,
                    Month = date.Month,
                    Day = date.Day,
                    Count = eventData.ContainsKey(date) ? eventData[date] : 0
                });
            }

            return result;
        }
        #endregion

        #region donation theo thời gian
        public List<dynamic> StatisticsDonationByTime(string? startDate = null, string? endDate = null)
        {
            // Xác định khoảng thời gian
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly   = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime   = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly     = DateOnly.FromDateTime(startDateTime);
                endDateOnly       = DateOnly.FromDateTime(endDateTime);
            }

            // Lấy dữ liệu, group theo ngày và tính tổng Amount
            var data = _db.Donations
                .Where(d => d.DonationDate.HasValue)
                .AsEnumerable() // xử lý DateOnly ở client-side
                .Where(d => DateOnly.FromDateTime(d.DonationDate.Value) >= startDateOnly
                        && DateOnly.FromDateTime(d.DonationDate.Value) <= endDateOnly)
                .GroupBy(d => DateOnly.FromDateTime(d.DonationDate.Value))
                .Select(g => new
                {
                    Date        = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .ToDictionary(x => x.Date, x => x.TotalAmount);

            // Tạo danh sách tất cả ngày trong khoảng, nếu không có thì mặc định 0
            var result = new List<dynamic>();
            for (var date = startDateOnly; date <= endDateOnly; date = date.AddDays(1))
            {
                result.Add(new
                {
                    Year        = date.Year,
                    Month       = date.Month,
                    Day         = date.Day,
                    TotalAmount = data.ContainsKey(date) ? data[date] : 0
                });
            }

            return result;
        }
        #endregion

        

        #region Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account", new { area = "" });
        }
        #endregion
    }
}
