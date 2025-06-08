using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.Areas.Admins.Data;
using Volunteer_website.Models;
using X.PagedList.Extensions;

namespace Volunteer_website.Areas.Admins.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    [Route("Admin/EventManager")] 
    public class EventManagerController : Controller
    {
        private readonly VolunteerManagementContext _db;
        public EventManagerController(VolunteerManagementContext context) 
        {
            _db = context;
        }

        #region Hiển thị sự kiện
        [Route("Event")]
        public IActionResult Event(int? page)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;

            var lstEvent = _db.Events.AsNoTracking()
                            .OrderBy(x => x.EventId)
                            .ToPagedList(pageNumber, pageSize);
            return View(lstEvent);
        }
        #endregion

        #region Duyệt sự kiện 

        [HttpPost]
        [Route("acceptEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> acceptEvent([FromBody] EventRequest request)
        {
            try
            {
                var EventId = request.EventId;
                var existingEvent = _db.Events.FirstOrDefault(ev => ev.EventId == EventId);
                if (existingEvent == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // chuyển trạng thái status
                existingEvent.Status = "ACCEPTED";

                _db.Update(existingEvent);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Event accepted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("rejectEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> rejectEvent([FromBody] EventRequest request)
        {
            try
            {
                var existingEvent = _db.Events.FirstOrDefault(ev => ev.EventId == request.EventId);
                if (existingEvent == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // chuyển trạng thái status
                existingEvent.Status = "REJECTED";

                _db.Update(existingEvent);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Event rejected successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region Xem chi tiet su kien
        [HttpGet]
        public async Task<IActionResult> EventDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Event ID is required.");
            }

            try
            {

                var eventDetail = await _db.Events
                    .Where(x => x.EventId == id)
                    .FirstOrDefaultAsync();

                if (eventDetail == null)
                {
                    return NotFound("Event not found.");
                }


                var orgName = await _db.Organizations
                    .Where(o => o.OrgId == eventDetail.OrgId)
                    .Select(o => o.Name)
                    .FirstOrDefaultAsync() ?? "Unknown Organization";
                var org = await _db.Organizations
                    .Where(o => o.OrgId == eventDetail.OrgId)
                    .FirstOrDefaultAsync();
                var imgPath = "/images/default.jpg";
                if(org!.ImagePath != null) imgPath = org.ImagePath;


                var registrations = await _db.Registrations
                    .Where(r => r.EventId == id)
                    .OrderByDescending(r => r.RegisterAt)
                    .Take(10)
                    .Join(
                        _db.Volunteers,
                        r => r.VolunteerId,
                        v => v.VolunteerId,
                        (r, v) => new
                        {
                            Name = v.Name ?? "Unknown",
                            Time = r.RegisterAt.HasValue ? r.RegisterAt.Value.ToString("dd/MM/yyyy") : "N/A"
                        })
                    .ToListAsync();


                var donations = await _db.Donations
                    .Where(d => d.EventId == id)
                    .OrderByDescending(d => d.DonationDate)
                    .Take(10)
                    .Join(
                        _db.Volunteers,
                        d => d.VolunteerId,
                        v => v.VolunteerId,
                        (d, v) => new
                        {
                            Name = v.Name ?? "Unknown",
                            Amount = d.Amount,
                            Time = d.DonationDate.HasValue ? d.DonationDate.Value.ToString("dd/MM/yyyy") : "N/A"
                        })
                    .ToListAsync();


                ViewBag.OrgName = orgName;
                ViewBag.Participants = registrations;
                ViewBag.ImgPath = imgPath;
                ViewBag.Donations = donations;
                ViewBag.RegisteredCount = await _db.Registrations.CountAsync(r => r.EventId == id);

                return View(eventDetail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex);
                return StatusCode(500, "An error occurred while fetching event details.");
            }
        }
        #endregion

    }
}