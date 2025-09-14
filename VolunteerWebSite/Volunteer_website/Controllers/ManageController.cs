using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Volunteer_website.Models;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Volunteer_website.Controllers
{
    [Authorize("Volunteer")]
    public class ManageController : Controller
    {
        private readonly VolunteerManagementContext _context;

        public ManageController(VolunteerManagementContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Registered_Event()
        {
            string volunteerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(volunteerId))
            {
                return RedirectToAction("Login", "Account");
            }

            var registeredEvents = _context.Registrations
                .Include(ev => ev.Event)
                .Include(ev => ev.Evaluations) 
                .Where(ev => ev.VolunteerId == volunteerId)
                .ToList();

            return View(registeredEvents);
        }

        [HttpPost]
        public IActionResult CancelRegistration(string eventId)
        {
            string volunteerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(volunteerId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này" });
            }

            var registration = _context.Registrations
                .FirstOrDefault(ev => ev.EventId == eventId && ev.VolunteerId == volunteerId);

            if (registration == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đăng ký" });
            }

            if (registration.Status == "ACCEPTED")
            {
                return Json(new { success = false, message = "Không thể hủy đăng ký đã được duyệt" });
            }

            try
            {
                _context.Registrations.Remove(registration);
                _context.SaveChanges();

                return Json(new { success = true, message = "Hủy đăng ký thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi hủy đăng ký: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetRegisteredEventCount()
        {
            string volunteerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(volunteerId))
            {
                return Json(new { count = 0 });
            }

            int count = _context.Registrations
                .Where(ev => ev.VolunteerId == volunteerId)
                .Count();
            return Json(new { count });
        }
    }
}