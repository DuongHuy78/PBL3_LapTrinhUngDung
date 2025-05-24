using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Volunteer_website.Models;
using Volunteer_website.ViewModels;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.Filters;
using Volunteer_website.Services;
namespace Volunteer_website.Controllers
{
    public class DonationsController : Controller
    {
        private readonly VolunteerManagementContext _context;
        private readonly ILogger<DonationsController> _logger;
        private readonly IVnPayService _vnPayService;
        public DonationsController(VolunteerManagementContext context, ILogger<DonationsController> logger, IVnPayService vnPayService)
        {
            _context = context;
            _logger = logger;
            _vnPayService = vnPayService;
        }

        //// GET: Donations/Create
        //[Authorize]
        //public IActionResult Create(int eventId)
        //{
        //    // Get the event from database
        //    var eventModel = _context.Events.Find(eventId);
        //    if (eventModel == null)
        //    {
        //        return NotFound();
        //    }

        //    var model = new DonationModel
        //    {
        //        EventId = eventId
        //    };

        //    // Pre-fill user info for all authenticated users
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var user = _context.Volunteers.Find(userId);

        //    if (user != null)
        //    {
        //        model.Name = user.Name;
        //        model.Email = user.Email;
        //        model.Phone = user.PhoneNumber;
        //        model.Address = user.Address;
        //    }

        //    return View(model);
        //}

        // POST: Donations/Create for Volunteer
        [OnlyVolunteer]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DonationModel donationModel)
        {
            if (!ModelState.IsValid)
            {
                return View(donationModel);
            }

            try
            {
                // Lấy thông tin người dùng từ session
                var userId = HttpContext.Session.GetString("UserId");
                var role = HttpContext.Session.GetString("UserRole");
               
                if (role == "volunteer" && !string.IsNullOrEmpty(userId))
                {
                    donationModel.Volunteer_Id = userId;
                }
                string gen_id = Guid.NewGuid().ToString();
                var donation = new Models.Donation
                {
                    DonationId = gen_id,
                    EventId = donationModel.Event_id,
                    VolunteerId = donationModel.Volunteer_Id,
                    DonationDate = DateTime.Now,
                    Amount = donationModel.Amount,
                    Message = donationModel.Note,
                };
                _logger.LogInformation($"DonationDate before save: {donation.DonationDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "null"}");


                _context.Donations.Add(donation);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Cảm ơn bạn đã đóng góp cho sự kiện!";
                return RedirectToAction("Detail_Event", "Home", new { id = donationModel.Event_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo donation.");
                TempData["Error"] = "Đã xảy ra lỗi khi xử lý yêu cầu của bạn.";
                return RedirectToAction("Error");
            }
        }

        
        public IActionResult Index()
        {
            

            return View();
        }
        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            return Json(response);
        }

    }
}