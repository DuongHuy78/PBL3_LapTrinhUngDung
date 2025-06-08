using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.Areas.Admins.Data;
using Volunteer_website.Models;
using X.PagedList.Extensions;

namespace Volunteer_website.Areas.Admins.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class VolunteersController : Controller
    {
        private readonly VolunteerManagementContext _context;

        public VolunteersController(VolunteerManagementContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1)
        {
            int pageSize = 8;
            int PageNumber = page;

            var listVolunteers =  _context.Volunteers
                .OrderBy(v => v.VolunteerId)
                .ToPagedList(PageNumber, pageSize);
            return View(listVolunteers);
        }

        #region GetVolunteerDetails
        public IActionResult GetVolunteerDetails(string id)
        {
            try
            {
                var volunteer = _context.Volunteers
                    .FirstOrDefault(v => v.VolunteerId == id);
                Console.WriteLine(id);
                Console.WriteLine(volunteer!.VolunteerId);
                if (volunteer == null)
                {
                    return Json(new { success = false, message = "Volunteer not found" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        volunteerId = volunteer.VolunteerId,
                        name = volunteer.Name,
                        email = volunteer.Email,
                        phoneNumber = volunteer.PhoneNumber,
                        dateOfBirth = volunteer.DateOfBirth?.ToString("dd/MM/yyyy"),
                        gender = volunteer.Gender.HasValue ?
                            (volunteer.Gender.Value ? "Male" : "Female") : "Not specified",
                        imagePath = volunteer.ImagePath,
                        address = volunteer.Address
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while fetching volunteer details",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region List Event Reg
        public IActionResult ListEventReg(string volId, int? page)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;

            // Lấy danh sách các EventId mà volunteer này đã đăng ký
            var eventIds = _context.Registrations
                .Where(r => r.VolunteerId == volId)
                .Select(r => r.EventId)
                .ToList();

            // Đếm số lượng thành viên cho từng EventId trong danh sách trên
            var memberCounts = _context.Registrations
                .Where(r => eventIds.Contains(r.EventId))
                .GroupBy(r => r.EventId)
                .Select(g => new { EventId = g.Key, MemberCount = g.Count() })
                .Where(x => x.EventId != null)
                .ToDictionary(x => x.EventId!, x => x.MemberCount);

            // Tính tổng amount cho từng EventId trong danh sách trên
            var totalAmounts = _context.Donations
                .Where(d => eventIds.Contains(d.EventId))
                .GroupBy(d => d.EventId)
                .Select(g => new { EventId = g.Key, TotalAmount = g.Sum(d => d.Amount ?? 0) })
                .Where(x => x.EventId != null)
                .ToDictionary(x => x.EventId!, x => x.TotalAmount);

            var listEvents = _context.Registrations
                .Where(r => r.VolunteerId == volId)
                .Select(r => new ListEvent
                {
                    EventId = r.Event!.EventId,
                    OrgId = r.Event.OrgId,
                    TypeEvent = r.Event.TypeEvent!.Name,
                    Name = r.Event.Name,
                    Description = r.Event.Description,
                    DayBegin = r.Event.DayBegin,
                    DayEnd = r.Event.DayEnd,
                    Location = r.Event.Location,
                    TargetMember = r.Event.TargetMember,
                    TargetFunds = r.Event.TargetFunds,
                    IsActive = r.Event.IsActive,
                    Status = r.Event.Status,
                    CurrentMember = memberCounts.ContainsKey(r.Event.EventId) ? memberCounts[r.Event.EventId] : 0,
                    CurrentFunds = totalAmounts.ContainsKey(r.Event.EventId) ? totalAmounts[r.Event.EventId] : 0
                })
                .OrderBy(x => x.EventId)
                .ToPagedList(pageNumber, pageSize);


            ViewData["volId"] = volId;

            return View(listEvents);
        }
        #endregion
    }
}
