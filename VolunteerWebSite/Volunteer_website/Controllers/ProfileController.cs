using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.Models;
using System.Web.Helpers;
using Volunteer_website.ViewModels;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MyCommerce.Models;
using Volunteer_website.Helpers;

namespace Volunteer_website.Controllers
{
    [Authorize("Volunteer")]
    public class ProfileController : Controller
    {
        private readonly VolunteerManagementContext _context;

        public ProfileController(VolunteerManagementContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Update_Infor()
        {
            string volunteerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(volunteerId))
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Manage");
            }

            var volunteer = _context.Volunteers.FirstOrDefault(v => v.VolunteerId == volunteerId);
            var user = _context.Users.FirstOrDefault(u => u.UserId == volunteerId);

            if (volunteer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu người dùng.";
                return RedirectToAction("Manage");
            }

            var model = new Update_ContactModel
            {
                UserName = user.UserName,
                VolunteerId = volunteer.VolunteerId,
                Name = volunteer.Name,
                PhoneNumber = volunteer.PhoneNumber,
                Email = volunteer.Email,
                Address = volunteer.Address,
                DateOfBirth = volunteer.DateOfBirth,
                Gender = volunteer.Gender,
                AvatarPath = volunteer.ImagePath
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update_Infor(Update_ContactModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.VolunteerId == model.VolunteerId);
            if (volunteer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tình nguyện viên.";
                return RedirectToAction("Update_Infor");
            }

            // Kiểm tra trùng email & phone
            var existingPhone = await _context.Volunteers
                .Where(v => v.VolunteerId != model.VolunteerId && v.PhoneNumber == model.PhoneNumber)
                .FirstOrDefaultAsync();
            if (existingPhone != null)
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại đã được sử dụng.");
                return View(model);
            }

            var existingEmail = await _context.Volunteers
                .Where(v => v.VolunteerId != model.VolunteerId && v.Email == model.Email)
                .FirstOrDefaultAsync();
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }

            // Cập nhật avatar nếu có
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(model.AvatarFile.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (JPG, PNG, GIF)");
                    return View(model);
                }

                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                volunteer.ImagePath = $"~/uploads/{fileName}";
            }

            // Cập nhật thông tin cá nhân và liên hệ
            volunteer.Name = model.Name;
            volunteer.Gender = model.Gender ?? false;
            volunteer.DateOfBirth = model.DateOfBirth;
            volunteer.PhoneNumber = model.PhoneNumber;
            volunteer.Email = model.Email;
            volunteer.Address = model.Address;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật: " + ex.Message;
            }

            return RedirectToAction("Update_Infor");
        }


        [HttpGet]
        public IActionResult Change_PassWord()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Change_PassWord(Change_Password Temp)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.ErrorMessage = "Bạn chưa đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Không tìm thấy người dùng.";
                return View("Change_PassWord");
            }

            Console.WriteLine("UserId: " + userId); // Debugging line
            Console.WriteLine("OldPassword (entered): " + Temp.OldPassword); // Debugging line

            // Kiểm tra mật khẩu cũ
            if (string.IsNullOrEmpty(user.Password))
            {
                ViewBag.ErrorMessage = "Mật khẩu hiện tại không tồn tại trong hệ thống.";
                return View("Change_PassWord");
            }

            // So sánh mật khẩu cũ bằng cách mã hóa Temp.OldPassword và so với user.Password
            string hashedOldPassword = Temp.OldPassword.ToMd5Hash(user.RandomKey);
            if (user.Password != hashedOldPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu cũ không đúng.";
                return View("Change_PassWord");
            }

            // Kiểm tra mật khẩu mới và xác nhận
            if (Temp.NewPassword != Temp.ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return View("Change_PassWord");
            }

            // Cập nhật mật khẩu trong transaction
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Tạo RandomKey mới
                    string newRandomKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)); // 128-bit random key
                    user.Password = Temp.NewPassword.ToMd5Hash(newRandomKey);
                    user.RandomKey = newRandomKey;
                    _context.SaveChanges();
                    transaction.Commit();

                    ViewBag.Message = "Đổi mật khẩu thành công.";
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    ViewBag.ErrorMessage = $"Lỗi cơ sở dữ liệu khi đổi mật khẩu: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.ErrorMessage = $"Lỗi hệ thống khi đổi mật khẩu: {ex.Message}";
                }
            }

            return View("Change_PassWord");
        }




        private string GenerateRandomKey()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Range(0, 8) 
                                      .Select(_ => chars[random.Next(chars.Length)])
                                      .ToArray());
        }

        
        private bool IsUserInRole(string role)
        {
            var currentRole = HttpContext.Session.GetString("UserRole");
            return currentRole != null && currentRole.Equals(role, StringComparison.OrdinalIgnoreCase);
        }
        public IActionResult Manage()
        {
            return View();
        }
       
    }
}