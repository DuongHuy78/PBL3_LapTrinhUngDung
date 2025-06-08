using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Volunteer_website.Models;

namespace Volunteer_website.Areas.Organizations.Controllers
{
    [Area("Organization")]
    [Route("Organization/[controller]/[action]")]
    [Authorize("Org")]
    public class StatisticsController : Controller
    {
        private readonly VolunteerManagementContext _db;

        public StatisticsController(VolunteerManagementContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                return RedirectToAction("Login", "Account");
            }

            var eventIds = _db.Events
                .Where(e => e.OrgId == orgId)
                .Select(e => e.EventId)
                .ToList();

            ViewBag.TotalVolunteers = _db.Registrations
                .Where(r => eventIds.Contains(r.EventId))
                .Select(r => r.VolunteerId)
                .Distinct()
                .Count();

            ViewBag.TotalEvents = _db.Events
                .Count(e => e.OrgId == orgId);

            ViewBag.TotalRegistrations = _db.Registrations
                .Count(r => eventIds.Contains(r.EventId));

            ViewBag.UncompletedEvaluations = _db.Evaluations
                .Count(e => !e.IsCompleted && eventIds.Contains(e.Reg.EventId));

            // Handle default dates  
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var defaultEndDateTime = DateTime.Now;
                var defaultStartDateTime = defaultEndDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(defaultStartDateTime);
                endDateOnly = DateOnly.FromDateTime(defaultEndDateTime);
            }

            ViewBag.StartDate = startDateOnly.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDateOnly.ToString("yyyy-MM-dd");

            // Registration statistics by time  
            var statisticsData = StatisticsRegistrationByTime(searchValue, startDate, endDate);
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

            // Donation statistics by time  
            var donationStatistics = StatisticsDonationByTime(searchValue, startDate, endDate);
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

            // Registration statistics by event  
            var registrationByEventData = StatisticsRegistrationByEvent(searchValue, startDate, endDate);
            var registrationByEventChartData = registrationByEventData.Select(x => new
            {
                Label = x.EventName,
                Count = x.Count
            }).ToList();

            ViewBag.RegistrationByEventStatistics = new
            {
                Labels = registrationByEventChartData.Select(x => x.Label).ToArray(),
                Data = registrationByEventChartData.Select(x => x.Count).ToArray()
            };

            // Donation statistics by event  
            var donationByEventData = StatisticsDonationByEvent(searchValue, startDate, endDate);
            var donationByEventChartData = donationByEventData.Select(x => new
            {
                Label = x.EventName,
                TotalAmount = x.TotalAmount
            }).ToList();

            ViewBag.DonationByEventStatistics = new
            {
                Labels = donationByEventChartData.Select(x => x.Label).ToArray(),
                Data = donationByEventChartData.Select(x => x.TotalAmount).ToArray()
            };

            // Event summary  
            var eventSummaryData = StatisticsEventSummary(searchValue, startDate, endDate);
            ViewBag.EventSummary = eventSummaryData;

            // Fix totalRegistrationsByDate to avoid ToDateTime in query  
            var startDateTime = startDateOnly.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDateOnly.ToDateTime(TimeOnly.MinValue);
            var totalRegistrationsByDate = _db.Registrations
                .Where(r => eventIds.Contains(r.EventId) && r.RegisterAt.HasValue)
                .AsEnumerable() // Force client-side evaluation  
                .Count(r => r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) >= startDateTime &&
                            r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) <= endDateTime);

            ViewBag.TotalRegistrationsByDate = totalRegistrationsByDate;

            // Total donation by date  
            var totalDonationByDate = _db.Donations
                .Where(d => eventIds.Contains(d.EventId) && d.DonationDate.HasValue)
                .AsEnumerable()
                .Sum(d => d.DonationDate!.Value >= startDateTime && d.DonationDate!.Value <= endDateTime ? d.Amount ?? 0 : 0);

            ViewBag.TotalDonationByDate = totalDonationByDate;

            return View();
        }

        #region Thống kê số lượt đăng ký theo thời gian
        public List<dynamic> StatisticsRegistrationByTime(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var defaultEndDateTime = DateTime.Now;
                var defaultStartDateTime = defaultEndDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(defaultStartDateTime);
                endDateOnly = DateOnly.FromDateTime(defaultEndDateTime);
            }

            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();
            var startDateTime = startDateOnly.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDateOnly.ToDateTime(TimeOnly.MinValue);

            var data = _db.Registrations
                .Where(r => eventIds.Contains(r.EventId) && r.RegisterAt.HasValue)
                .AsEnumerable() // Force client-side evaluation
                .Where(r => r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) >= startDateTime &&
                            r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) <= endDateTime)
                .GroupBy(r => new
                {
                    Year = r.RegisterAt!.Value.Year,
                    Month = r.RegisterAt!.Value.Month,
                    Day = r.RegisterAt!.Value.Day
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ThenBy(x => x.Day)
                .ToList();

            return data.Cast<dynamic>().ToList();
        }
        #endregion

        #region Thống kê số tiền Donation theo thời gian
        public List<dynamic> StatisticsDonationByTime(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateTime startDateTime, endDateTime;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateTime = DateTime.Parse(startDate);
                endDateTime = DateTime.Parse(endDate);
            }
            else
            {
                endDateTime = DateTime.Now;
                startDateTime = endDateTime.AddDays(-30);
            }

            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();

            var data = _db.Donations
                .Where(r => eventIds.Contains(r.EventId) && r.DonationDate.HasValue)
                .AsEnumerable() // Force client-side evaluation
                .Where(r => r.DonationDate!.Value >= startDateTime && r.DonationDate!.Value <= endDateTime)
                .GroupBy(r => new
                {
                    Year = r.DonationDate!.Value.Year,
                    Month = r.DonationDate!.Value.Month,
                    Day = r.DonationDate!.Value.Day
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    TotalAmount = g.Sum(d => d.Amount ?? 0)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ThenBy(x => x.Day)
                .ToList();

            return data.Cast<dynamic>().ToList();
        }
        #endregion

        #region Số lượt đăng ký theo sự kiện
        public List<dynamic> StatisticsRegistrationByEvent(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var defaultEndDateTime = DateTime.Now;
                var defaultStartDateTime = defaultEndDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(defaultStartDateTime);
                endDateOnly = DateOnly.FromDateTime(defaultEndDateTime);
            }

            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();
            var startDateTime = startDateOnly.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDateOnly.ToDateTime(TimeOnly.MinValue);

            var data = _db.Registrations
                .Where(r => eventIds.Contains(r.EventId) && r.RegisterAt.HasValue)
                .AsEnumerable()
                .Where(r => r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) >= startDateTime &&
                            r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) <= endDateTime)
                .GroupBy(r => r.EventId)
                .Join(_db.Events,
                      regGroup => regGroup.Key,
                      evt => evt.EventId,
                      (regGroup, evt) => new
                      {
                          EventName = evt.Name ?? "Sự kiện không tên",
                          Count = regGroup.Count()
                      })
                .OrderBy(x => x.EventName)
                .ToList();

            return data.Cast<dynamic>().ToList();
        }
        #endregion

        #region Số tiền ủng hộ theo sự kiện
        public List<dynamic> StatisticsDonationByEvent(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateTime startDateTime, endDateTime;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateTime = DateTime.Parse(startDate);
                endDateTime = DateTime.Parse(endDate);
            }
            else
            {
                endDateTime = DateTime.Now;
                startDateTime = endDateTime.AddDays(-30);
            }

            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();

            var data = _db.Donations
                .Where(d => eventIds.Contains(d.EventId) && d.DonationDate.HasValue)
                .AsEnumerable()
                .Where(d => d.DonationDate!.Value >= startDateTime && d.DonationDate!.Value <= endDateTime)
                .GroupBy(d => d.EventId)
                .Join(_db.Events,
                      donateGroup => donateGroup.Key,
                      evt => evt.EventId,
                      (donateGroup, evt) => new
                      {
                          EventName = evt.Name ?? "Sự kiện không tên",
                          TotalAmount = donateGroup.Sum(d => d.Amount ?? 0)
                      })
                .OrderBy(x => x.EventName)
                .ToList();

            return data.Cast<dynamic>().ToList();
        }
        #endregion

        #region Bảng thống kê tổng quát
        public List<dynamic> StatisticsEventSummary(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateTime startDateTime, endDateTime;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateTime = DateTime.Parse(startDate);
                endDateTime = DateTime.Parse(endDate);
            }
            else
            {
                endDateTime = DateTime.Now;
                startDateTime = endDateTime.AddDays(-30);
            }

            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var events = eventQuery.ToList();

            var registrationCounts = _db.Registrations
                .Where(r => events.Select(e => e.EventId).Contains(r.EventId) && r.RegisterAt.HasValue)
                .AsEnumerable()
                .Where(r => r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) >= startDateTime &&
                            r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue) <= endDateTime)
                .GroupBy(r => r.EventId)
                .Select(g => new { EventId = g.Key ?? string.Empty, Count = g.Count() })
                .ToDictionary(g => g.EventId, g => g.Count);

            var donationAmounts = _db.Donations
                .Where(d => events.Select(e => e.EventId).Contains(d.EventId) && d.DonationDate.HasValue)
                .AsEnumerable()
                .Where(d => d.DonationDate!.Value >= startDateTime && d.DonationDate!.Value <= endDateTime)
                .GroupBy(d => d.EventId)
                .Select(g => new { EventId = g.Key ?? string.Empty, TotalAmount = g.Sum(d => d.Amount ?? 0) })
                .ToDictionary(g => g.EventId, g => g.TotalAmount);

            var data = events.Select(e => new
            {
                EventName = e.Name ?? "Sự kiện không tên",
                RegistrationCount = registrationCounts.ContainsKey(e.EventId) ? registrationCounts[e.EventId] : 0,
                DonationAmount = donationAmounts.ContainsKey(e.EventId) ? donationAmounts[e.EventId] : 0
            })
            .OrderBy(x => x.EventName)
            .ToList();

            Console.WriteLine($"Event Summary Statistics: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

            return data.Cast<dynamic>().ToList();
        }
        #endregion

        [HttpGet]
        public IActionResult ExportEventSummaryReport(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            Console.WriteLine($"ExportEventSummaryReport called with searchValue: {searchValue}, startDate: {startDate}, endDate: {endDate}");

            var data = StatisticsEventSummary(searchValue, startDate, endDate);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Thống kê sự kiện");

                worksheet.Cells[1, 1].Value = "Thống kê tổng hợp sự kiện";
                worksheet.Cells[1, 1, 1, 3].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                string formattedStartDate = string.IsNullOrEmpty(startDate) ? DateTime.Now.AddDays(-30).ToString("dd/MM/yyyy") : DateOnly.Parse(startDate).ToString("dd/MM/yyyy");
                string formattedEndDate = string.IsNullOrEmpty(endDate) ? DateTime.Now.ToString("dd/MM/yyyy") : DateOnly.Parse(endDate).ToString("dd/MM/yyyy");
                worksheet.Cells[2, 1].Value = $"Thống kê từ ngày {formattedStartDate} đến ngày {formattedEndDate}";
                worksheet.Cells[2, 1, 2, 3].Merge = true;
                worksheet.Cells[2, 1].Style.Font.Size = 12;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[3, 1].Value = "Tên sự kiện";
                worksheet.Cells[3, 2].Value = "Số lượt tham gia";
                worksheet.Cells[3, 3].Value = "Số tiền ủng hộ (VND)";
                worksheet.Cells[3, 1, 3, 3].Style.Font.Bold = true;
                worksheet.Cells[3, 1, 3, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[3, 1, 3, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                worksheet.Cells[3, 1, 3, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                int row = 4;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.EventName;
                    worksheet.Cells[row, 2].Value = item.RegistrationCount;
                    worksheet.Cells[row, 3].Value = item.DonationAmount;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0";
                    row++;
                }

                worksheet.Cells[3, 1, row - 1, 3].AutoFitColumns();

                worksheet.Cells[3, 1, row - 1, 3].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[3, 1, row - 1, 3].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[3, 1, row - 1, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[3, 1, row - 1, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"ThongKeSuKien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}