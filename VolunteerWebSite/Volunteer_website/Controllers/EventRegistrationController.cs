using Microsoft.AspNetCore.Mvc;
using Volunteer_website.ViewModels;
using Volunteer_website.Models;

namespace Volunteer_website.Controllers
{
    
    public class EventRegistrationController : Controller
    {
        private readonly VolunteerManagementContext _context;

        public EventRegistrationController(VolunteerManagementContext context)
        {
            _context = context;
        }
        [HttpGet("EventRegistration/Register/{id}")] 
        public IActionResult RegisterGet(string id)
        {
            return RedirectToAction("Detail_Event", "Home", new { id = id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string id)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            // Kiểm tra đã đăng ký chưa
            var existed = _context.Registrations
             .FirstOrDefault(r => r.EventId == id && r.VolunteerId == userId );

            if (existed != null)
            {
                TempData["Message"] = "Bạn đã đăng ký sự kiện này rồi.";
                TempData.Keep("Message");
                return RedirectToAction("Detail_Event","Home", new { id = id });
            }

            var registration = new Registration
            {
                RegId = Guid.NewGuid().ToString().Substring(0, 5),
                EventId = id,
                VolunteerId = userId,
                RegisterAt = DateOnly.FromDateTime(DateTime.Today),
                Status = "Đang chờ duyệt" 

            };
            TempData["Message"] = null;

            _context.Registrations.Add(registration);
            _context.SaveChanges();

            TempData["Message"] = "Đăng ký tham gia sự kiện thành công!";
            TempData.Keep("Message");

            return RedirectToAction("Detail_Event", "Home", new { id = id });



        }

    }
}

