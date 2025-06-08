using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCommerce.Models;
using Volunteer_website.Helpers;
using Volunteer_website.Models;

namespace Volunteer_website.Areas.Admins.Controllers
{
    [Authorize("Admin")]
    [Area("Admin")]
    public class ProfileController : Controller
    {
        private readonly VolunteerManagementContext _db;

        public ProfileController(VolunteerManagementContext context)
        {
            _db = context;
        }

        #region hiển thị thông tin
        public async Task<IActionResult> Profile()
        {
            var id = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (id == null)
            {
                return NotFound();
            }

            var admin = await _db.Admins
                .FirstOrDefaultAsync(m => m.AdminId == id);
            if (admin == null)
            {
                return NotFound();
            }

            return View(admin);
        }
        #endregion

        #region chỉnh sửa thông tin cá nhân
        public async Task<IActionResult> ProfileEdit()
        {
            var id = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (id == null)
            {
                return NotFound();
            }

            var admin = await _db.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }
            return View(admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileEdit([Bind("AdminId,Name,ImgPath,Email")] Models.Admin admin, IFormFile? imgPath)
        {
            var id = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (id != admin.AdminId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == admin.AdminId);
                    var currentAdmin = await _db.Admins.FirstOrDefaultAsync(u => u.AdminId == admin.AdminId);
                    if(currentAdmin == null)
                    {
                        return NotFound();
                    }

                    if (currentUser == null)
                    {
                        return NotFound();
                    }

                    var isNotValidEmailAdmins = await _db.Admins.Where(e => e.AdminId != admin.AdminId)
                        .AnyAsync(e => e.Email == admin.Email);
                    var isNotValidEmailVolunteers = await _db.Volunteers.AnyAsync(e => e.Email == admin.Email);
                    var isNotValidEmailOrg = await _db.Organizations.AnyAsync(e => e.Email == admin.Email);

                    if (isNotValidEmailOrg || isNotValidEmailVolunteers || isNotValidEmailAdmins)
                    {
                        TempData["ErrorMessage"] = "Email đã được sử dụng!";
                        return View(admin);
                    }

                    if (imgPath != null)
                    {
                        currentAdmin.ImgPath = await UpLoadImgService.UploadImg(imgPath!, "EventsImg");
                    }
                    currentAdmin.Name = admin.Name;
                    currentAdmin.Email = admin.Email;

                    _db.Update(currentAdmin);
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (_db.Admins.Any(e => e.AdminId == admin.AdminId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Profile));
            }
            return View(admin);
        }
        #endregion

        #region change password
        public async Task<IActionResult> ChangePassword()
        {
            var id = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (id == null)
            {
                return NotFound();
            }

            var admin = await _db.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string newUsername, string newPassword, string currentPassword)
        {
            if(newPassword == null && newUsername == null)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập các thông tin muốn cập nhật!";
            }
            var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (adminId == null)
            {
                return NotFound();
            }

            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == adminId);
            var currentAdmin = await _db.Admins.FirstOrDefaultAsync(u => u.AdminId == adminId);

            if (currentAdmin == null)
            {
                return NotFound();
            }

            if (currentUser == null)
            {
                return NotFound();
            }

            var isNotValidUserNameUsers = await _db.Users.Where(e => e.UserId != currentAdmin.AdminId)
                        .AnyAsync(e => e.UserName == currentAdmin.Name);
            if (isNotValidUserNameUsers)
            {
                TempData["ErrorMessage"] = "UserName đã được sử dụng!";
                return View();
            }

            if (currentUser.Password == currentPassword.ToMd5Hash(currentUser.RandomKey))
            {   
                if(newPassword != null)
                {
                    currentUser.RandomKey = Util.GenerateRandomkey();
                    currentUser.Password = newPassword.ToMd5Hash(currentUser.RandomKey);
                }
                else if(newUsername != null)
                {
                    currentUser.UserName = newUsername;
                }

                _db.Users.Update(currentUser);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật thành công!";
                return RedirectToAction(nameof(Profile)); // Redirect về Profile sau khi thành công
            }
            else
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại bạn đã nhập sai. Vui lòng thử lại!";
            }
            return View();
        }
        #endregion
    }
}
