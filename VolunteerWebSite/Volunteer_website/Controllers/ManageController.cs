using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Volunteer_website.Models;


namespace Volunteer_website.Controllers
{
    public class ManageController:Controller
    {
        private readonly VolunteerManagementContext _context;
        public ManageController(VolunteerManagementContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Registered_Event()
        {
            var volunteerId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(volunteerId))
            {
                return RedirectToAction("Login", "Account");
            }

            var registeredEvents = _context.Registrations
                .Include(ev => ev.Event)
                .Where(ev => ev.VolunteerId == volunteerId)
                .ToList();

            return View(registeredEvents);
        }

        [HttpPost]
        public IActionResult CancelRegistration(string eventId)
        {
            var volunteerId = HttpContext.Session.GetString("UserId");
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
    }
}
