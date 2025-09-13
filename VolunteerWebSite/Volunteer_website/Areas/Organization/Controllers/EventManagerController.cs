using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Volunteer_website.Models;
using X.PagedList.Extensions;
using Volunteer_website.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Volunteer_website.Areas.Organizations.Controllers
{
    [Area("Organization")]
    [Route("[area]/[controller]/[action]")] // Sửa lại route template
    [Authorize("Org")]
    public class EventManagerController : Controller
    {

        private readonly VolunteerManagementContext _db;

        public EventManagerController(VolunteerManagementContext db)
        {
            _db = db;
        }


        #region Hiển thị sự kiện

        public IActionResult Index(int? page, string searchValue)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;
            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(orgId))
            {
                return RedirectToAction("Login", "Account"); // Hoặc xử lý lỗi khác nếu không có orgId
            }

            var query = _db.Events
                .Where(e => e.OrgId == orgId)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var lstEvent = query.OrderBy(x => x.EventId)
                                .ToPagedList(pageNumber, pageSize);

            ViewBag.SearchValue = searchValue;

            return View(lstEvent);
        }

        #endregion

        #region Thêm sự kiện
        [HttpGet]
        public IActionResult Create()
        {
            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(orgId) || !InputValidator.IsValidId(orgId))
            {
                TempData["Error"] = "Không tìm thấy thông tin tổ chức. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            ViewBag.OrgId = orgId;

            var lastEvent = _db.Events
                .OrderByDescending(e => e.EventId)
                .FirstOrDefault();

            string newEventId = "E0001";
            if (lastEvent != null)
            {
                string lastIdNumber = lastEvent.EventId.Replace("E", "");
                if (int.TryParse(lastIdNumber, out int number))
                {
                    newEventId = $"E{number + 1:D4}";
                }
            }

            var model = new Event
            {
                EventId = newEventId,
                OrgId = orgId,
            };

            // Lấy danh sách tất cả EventType từ bảng EventTypes
            var typeEventsList = _db.EventTypes.ToList();

            // Chuyển thành danh sách SelectListItem
            var typeEventsSelectList = typeEventsList
                .Select(et => new SelectListItem
                {
                    Value = et.TypeEventId,
                    Text = et.Name
                })
                .ToList();

            // Lưu vào ViewBag
            ViewBag.TypeEvents = typeEventsSelectList;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event eventModel, IFormFile? imagePath, IFormFileCollection? listImg)
        {
            // Biến để theo dõi nếu có lỗi liên quan đến file ảnh
            bool hasImageError = false;

            // Kiểm tra các trường bắt buộc và hợp lệ
            if (string.IsNullOrWhiteSpace(eventModel.Name) || !InputValidator.IsValidString(eventModel.Name))
            {
                TempData["Error"] = "Tên sự kiện không hợp lệ. Vui lòng chỉ sử dụng chữ cái (bao gồm tiếng Việt) và khoảng trắng.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (string.IsNullOrWhiteSpace(eventModel.TypeEventId) || !InputValidator.IsValidId(eventModel.TypeEventId))
            {
                TempData["Error"] = "Loại sự kiện không hợp lệ.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (!InputValidator.IsValidDate(eventModel.DayBegin, true))
            {
                TempData["Error"] = "Ngày bắt đầu không hợp lệ.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (!InputValidator.IsValidDate(eventModel.DayEnd, true))
            {
                TempData["Error"] = "Ngày kết thúc không hợp lệ.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (eventModel.DayBegin.HasValue && eventModel.DayEnd.HasValue && eventModel.DayBegin > eventModel.DayEnd)
            {
                TempData["Error"] = "Ngày bắt đầu phải trước ngày kết thúc.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (string.IsNullOrWhiteSpace(eventModel.Location))
            {
                TempData["Error"] = "Vị trí sự kiện không được để trống.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (!InputValidator.IsValidTarget(eventModel.TargetMember))
            {
                TempData["Error"] = "Số lượng thành viên mục tiêu không hợp lệ.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (!InputValidator.IsValidTarget(eventModel.TargetFunds))
            {
                TempData["Error"] = "Số tiền mục tiêu không hợp lệ.";
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (imagePath != null && !InputValidator.IsValidImagePath(imagePath.FileName))
            {
                TempData["Error"] = "Định dạng ảnh không hợp lệ. Vui lòng chọn lại file ảnh (.jpg, .jpeg, .png, .gif).";
                hasImageError = true;
                ReloadTypeEvents();
                return View(eventModel);
            }

            if (listImg != null && listImg.Any() && listImg.Any(img => !InputValidator.IsValidImagePath(img.FileName)))
            {
                TempData["Error"] = "Một hoặc nhiều ảnh trong danh sách không hợp lệ. Vui lòng chọn lại file ảnh (.jpg, .jpeg, .png, .gif).";
                hasImageError = true;
                ReloadTypeEvents();
                return View(eventModel);
            }

            // Upload ảnh nếu hợp lệ
            if (imagePath != null)
            {
                eventModel.ImagePath = await UpLoadImgService.UploadImg(imagePath, "EventsImg");
            }

            if (listImg != null && listImg.Any())
            {
                var uploadedImages = await UpLoadImgService.UploadListImg(listImg, "EventsImg");
                eventModel.ListImg = string.Join(",", uploadedImages);
            }

            // Nếu tất cả kiểm tra đều hợp lệ, lưu vào cơ sở dữ liệu
            eventModel.Status = "PENDING";
            _db.Add(eventModel);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sự kiện thành công";
            return RedirectToAction("Index");

            // Hàm hỗ trợ để reload danh sách TypeEvents
            void ReloadTypeEvents()
            {
                var typeEventsList = _db.EventTypes.ToList();
                ViewBag.TypeEvents = typeEventsList
                    .Select(et => new SelectListItem
                    {
                        Value = et.TypeEventId,
                        Text = et.Name
                    })
                    .ToList();
                ViewBag.OrgId = eventModel.OrgId;
                if (hasImageError)
                {
                    TempData["ImageWarning"] = "Vui lòng chọn lại file ảnh do lỗi định dạng.";
                }
            }
        }
        #endregion

        #region Chỉnh sửa sự kiện
        [HttpGet]
        public async Task<IActionResult> EditEvent(string id)
        {
            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId) || !InputValidator.IsValidId(orgId))
            {
                TempData["Error"] = "Không tìm thấy thông tin tổ chức. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (!InputValidator.IsValidId(id))
            {
                TempData["Error"] = "ID sự kiện không hợp lệ.";
                return NotFound();
            }

            var eventModel = await _db.Events.FindAsync(id);
            if (eventModel == null)
            {
                TempData["Error"] = "Không tìm thấy sự kiện.";
                return NotFound();
            }

            if (eventModel.OrgId != orgId)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa sự kiện này.";
                return Forbid();
            }

            ViewBag.OrgId = eventModel.OrgId;

            // Lấy danh sách tất cả EventType từ bảng EventTypes
            var typeEventsList = _db.EventTypes.ToList();

            // Chuyển thành danh sách SelectListItem
            var typeEventsSelectList = typeEventsList
                .Select(et => new SelectListItem
                {
                    Value = et.TypeEventId,
                    Text = et.Name
                })
                .ToList();

            // Lưu vào ViewBag
            ViewBag.TypeEvents = typeEventsSelectList;
            return View(eventModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(Event model, IFormFile? imagePath, IFormFileCollection? listImg)
        {
            // Biến để theo dõi nếu có lỗi liên quan đến file ảnh
            bool hasImageError = false;

            // Kiểm tra ID sự kiện
            if (!InputValidator.IsValidId(model.EventId))
            {
                TempData["Error"] = "ID sự kiện không hợp lệ.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            // Kiểm tra sự tồn tại của sự kiện
            var existingEvent = await _db.Events.FindAsync(model.EventId);
            if (existingEvent == null)
            {
                TempData["Error"] = "Không tìm thấy sự kiện.";
                ReloadTypeEvents(model.OrgId);
                return NotFound();
            }

            // Kiểm tra quyền chỉnh sửa
            var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (existingEvent.OrgId != orgId)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa sự kiện này.";
                ReloadTypeEvents(model.OrgId);
                return Forbid();
            }

            // Kiểm tra các trường bắt buộc và hợp lệ
            if (string.IsNullOrWhiteSpace(model.Name) || !InputValidator.IsValidString(model.Name))
            {
                TempData["Error"] = "Tên sự kiện không hợp lệ. Vui lòng chỉ sử dụng chữ cái (bao gồm tiếng Việt) và khoảng trắng.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.TypeEventId) || !InputValidator.IsValidId(model.TypeEventId))
            {
                TempData["Error"] = "Loại sự kiện không hợp lệ.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (!InputValidator.IsValidDate(model.DayBegin, true))
            {
                TempData["Error"] = "Ngày bắt đầu không hợp lệ.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (!InputValidator.IsValidDate(model.DayEnd, true))
            {
                TempData["Error"] = "Ngày kết thúc không hợp lệ.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (model.DayBegin.HasValue && model.DayEnd.HasValue && model.DayBegin > model.DayEnd)
            {
                TempData["Error"] = "Ngày bắt đầu phải trước ngày kết thúc.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Location))
            {
                TempData["Error"] = "Vị trí sự kiện không được để trống.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (!InputValidator.IsValidTarget(model.TargetMember))
            {
                TempData["Error"] = "Số lượng thành viên mục tiêu không hợp lệ.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (!InputValidator.IsValidTarget(model.TargetFunds))
            {
                TempData["Error"] = "Số tiền mục tiêu không hợp lệ.";
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (imagePath != null && !InputValidator.IsValidImagePath(imagePath.FileName))
            {
                TempData["Error"] = "Định dạng ảnh không hợp lệ. Vui lòng chọn lại file ảnh (.jpg, .jpeg, .png, .gif).";
                hasImageError = true;
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            if (listImg != null && listImg.Any() && listImg.Any(img => !InputValidator.IsValidImagePath(img.FileName)))
            {
                TempData["Error"] = "Một hoặc nhiều ảnh trong danh sách không hợp lệ. Vui lòng chọn lại file ảnh (.jpg, .jpeg, .png, .gif).";
                hasImageError = true;
                ReloadTypeEvents(model.OrgId);
                return View(model);
            }

            // Cập nhật các trường hợp lệ
            existingEvent.Name = model.Name;
            existingEvent.TargetMember = model.TargetMember;
            existingEvent.Status = model.Status;
            existingEvent.TypeEventId = model.TypeEventId;
            existingEvent.DayBegin = model.DayBegin;
            existingEvent.DayEnd = model.DayEnd;
            existingEvent.Location = model.Location;
            existingEvent.TargetFunds = model.TargetFunds;
            existingEvent.Description = model.Description;
            existingEvent.IsActive = model.IsActive;
            // Upload ảnh nếu có
            if (imagePath != null)
            {
                existingEvent.ImagePath = await UpLoadImgService.UploadImg(imagePath, "EventsImg");
            }

            if (listImg != null && listImg.Any())
            {
                var uploadedImages = await UpLoadImgService.UploadListImg(listImg, "EventsImg");
                existingEvent.ListImg = string.Join(",", uploadedImages);
            }

            // Lưu thay đổi vào cơ sở dữ liệu
            _db.Update(existingEvent);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thay đổi sự kiện thành công";
            return RedirectToAction("Index");

            // Hàm hỗ trợ để reload danh sách TypeEvents
            void ReloadTypeEvents(string orgId)
            {
                ViewBag.OrgId = orgId;
                var typeEventsList = _db.EventTypes.ToList();
                ViewBag.TypeEvents = typeEventsList
                    .Select(et => new SelectListItem
                    {
                        Value = et.TypeEventId,
                        Text = et.Name
                    })
                    .ToList();
                if (hasImageError)
                {
                    TempData["ImageWarning"] = "Vui lòng chọn lại file ảnh do lỗi định dạng.";
                }
            }
        }
        #endregion


        #region Xóa sự kiện
        [HttpPost]
      
        public IActionResult Delete( string id)
        {
            try
            {
                var eventToDelete = _db.Events.Find(id);
                if (eventToDelete == null)
                {
                    TempData["Error"] = "error";
                    TempData["Error"] = "Không tìm thấy sự kiện cần xóa";
                    return RedirectToAction("Index");
                }

               
                if (_db.Registrations.Any(x => x.EventId == id) || _db.Donations.Any(x => x.EventId == id))
                {
                    TempData["Error"] = "error";
                    TempData["Error"] = "Không thể xóa vì sự kiện đã có dữ liệu liên quan";
                    return RedirectToAction("Index");
                }

                _db.Events.Remove(eventToDelete);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "success";
                TempData["SuccessMessage"] = "Xóa sự kiện thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "error";
                TempData["Error"] = $"Lỗi khi xóa sự kiện: {ex.Message}";
            }

            return RedirectToAction("Index");
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

            var orgId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                return Unauthorized("User organization not found.");
            }

            try
            {
               
                var eventDetail = await _db.Events
                    .Where(x => x.EventId == id && x.OrgId == orgId)
                    .FirstOrDefaultAsync();

                if (eventDetail == null)
                {
                    return NotFound("Event not found.");
                }

               
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

              
                ViewBag.Participants = registrations;
                ViewBag.Donations = donations;

                return View(eventDetail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex);
                return StatusCode(500, "An error occurred while fetching event details.");
            }
        }
        #endregion


        //Tổng số người tham gia 

    }
}
