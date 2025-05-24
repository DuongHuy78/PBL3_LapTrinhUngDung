using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Volunteer_website.Models;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Volunteer_website.Areas.Organization.Controllers
{
    [Authorize("Org")]
    [Area("Organization")]
    [Route("Organization/RegistrationManager")] // Sửa lại route template
    public class RegistrationManagerController : Controller
    {
        private readonly VolunteerManagementContext _db;

        public RegistrationManagerController(VolunteerManagementContext db)
        {
            _db = db;
        }

        #region Danh sách người đăng kí tham gia
        public IActionResult Index(int? page, string searchValue)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;
            var query = _db.Registrations.AsNoTracking();
            if (!string.IsNullOrEmpty(searchValue))
            {
                var matchedEventIds = _db.Events
                                         .Where(e => e.Name.Contains(searchValue))
                                         .Select(e => e.EventId)
                                         .ToList();
                var matchedVolunteerIds = _db.Volunteers
                                             .Where(v => v.Name.Contains(searchValue))
                                             .Select(v => v.VolunteerId)
                                             .ToList();
                query = query.Where(r =>
                    matchedEventIds.Contains(r.EventId) ||
                    matchedVolunteerIds.Contains(r.VolunteerId));
            }
            var lstRegistered = query
                                .OrderBy(x => x.RegId)
                                .ToPagedList(pageNumber, pageSize);
            var volunteerIds = lstRegistered.Select(d => d.VolunteerId).Distinct().ToList();
            var volunteers = _db.Volunteers
                                .Where(v => volunteerIds.Contains(v.VolunteerId))
                                .ToDictionary(v => v.VolunteerId, v => v);

            var eventIds = lstRegistered.Select(d => d.EventId).Distinct().ToList();
            var events = _db.Events
                            .Where(e => eventIds.Contains(e.EventId))
                            .ToDictionary(e => e.EventId, e => e);
            ViewBag.Volunteers = volunteers;
            ViewBag.Events = events;
            ViewBag.SearchValue = searchValue;

            return View(lstRegistered);
        }

        #endregion

        #region Cập nhật trạng thái người tham gia
        [HttpGet]
        public IActionResult Update(string regId, string status)
        {
            try
            {
                // Validate status
                if (!new[] { "PENDING", "ACCEPTED", "REJECTED" }.Contains(status.ToUpper()))
                {
                    return Json(new { success = false, message = "Invalid status value" });
                }

                var registration = _db.Registrations.FirstOrDefault(r => r.RegId == regId);
                if (registration == null)
                {
                    return Json(new { success = false, message = "Registration not found" });
                }

                registration.Status = status.ToUpper();
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật trạng thái đăng ký thành công";
                return Json(new
                {
                    success = true,
                    message = status.ToUpper() == "ACCEPTED" ? "Registration approved" :
                              status.ToUpper() == "REJECTED" ? "Registration rejected" :
                              "Registration status updated"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }

        #endregion

        #region Xem chi tiết người tham gia
        [Route("GetVolunteerDetails")]
        public IActionResult GetVolunteerDetails(string id)
        {
            try
            {
                var volunteer = _db.Volunteers
                    .FirstOrDefault(v => v.VolunteerId == id);

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
    }
}



//