using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using MyCommerce.Models;
using Volunteer_website.Areas.Admins.Data;
using Volunteer_website.Helpers;
using Volunteer_website.Models;
using X.PagedList.Extensions;

namespace Volunteer_website.Areas.Admins.Controllers
{
    [Area("Admin")]
    [Route("Admin/Users")]
    public class UsersController : Controller
    {
        private readonly VolunteerManagementContext _context;

        public UsersController(VolunteerManagementContext context)
        {
            _context = context;
        }

        [Route("Index")]
        public IActionResult Index(int page = 1)
        {
            int pageSize = 8;
            int PageNumber = page;

            var listUsers = _context.Users
                .OrderBy(v => v.UserId)
                .ToPagedList(PageNumber, pageSize);
            return View(listUsers);
        }

        #region Ban and Unban user
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("BanUser")]
        public async Task<IActionResult> BanUser([FromBody] UserRequest request)
        {
            try
            {
                var VolunteerId = request.UserId;
                var existingUser = _context.Users.FirstOrDefault(ev => ev.UserId == VolunteerId);
                if (existingUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // chuyển trạng thái status
                existingUser.IsActive = false;

                _context.Update(existingUser);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "User Ban successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UnBanUser")]
        public async Task<IActionResult> UnBanUser([FromBody] UserRequest request)
        {
            try
            {
                var VolunteerId = request.UserId;
                var existingUser = _context.Users.FirstOrDefault(ev => ev.UserId == VolunteerId);
                if (existingUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // chuyển trạng thái status
                existingUser.IsActive = true;

                _context.Update(existingUser);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "User Unban successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region cấp quyền admin mới
        [HttpPost]
        [Route("PromoteToAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin([FromBody] UserRequest request)
        {
            var id = request.UserId;
            if (id == null)
            {
                TempData["ErrorMessage"] = "Truyền dữ liệu bị lỗi!";
                return RedirectToAction("Index");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(a => a.UserId == id);

            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại!!";
                return RedirectToAction("Index");
            }

            var isAdmin = await _context.Admins.AnyAsync(a => a.AdminId == id);
            var isVolunteer = await _context.Volunteers.FirstOrDefaultAsync(a => a.VolunteerId == id);
            var isOrg = await _context.Organizations.FirstOrDefaultAsync(a => a.OrgId == id);

            if (isAdmin)
            {
                TempData["ErrorMessage"] = "Người dùng đã là Admin";
                return RedirectToAction("Index");
            }

            try
            {
                string newnewIdAdmin;
                var LastAdmin = await _context.Admins
                    .OrderByDescending(HV => HV.AdminId)
                    .FirstOrDefaultAsync();
                if (LastAdmin != null)
                {
                    int temp = int.Parse(LastAdmin.AdminId.Substring(1));
                    temp++;
                    newnewIdAdmin = "A" + temp.ToString("D4");
                }
                else
                {
                    newnewIdAdmin = "A0001";
                }

                // Tạo mật khẩu ngẫu nhiên
                var randomPassword = Util.GenerateRandomkey(6);

                // Tạo tài khoản mới
                var user = new User();
                user.UserId = newnewIdAdmin;
                user.RandomKey = Util.GenerateRandomkey();
                user.UserName = Util.GenerateRandomkey(6);
                user.Password = randomPassword.ToMd5Hash(user.RandomKey);
                user.Role = 2;
                user.IsActive = true;
                user.CreateAt = DateOnly.FromDateTime(DateTime.Now);  // Ngày tạo tài khoản

                //tạo admin mới
                var newAdmin = new Admin();
                newAdmin.AdminId = newnewIdAdmin;
                newAdmin.Name = user.UserName;
                if (isOrg != null)
                {
                    newAdmin.Email = isOrg.Email;
                }
                else if (isVolunteer != null)
                {
                    newAdmin.Email = isVolunteer.Email;
                }
                newAdmin.ImgPath = "/images/default.jpg";

                _context.Users.Add(user);
                _context.Admins.Add(newAdmin);
                await _context.SaveChangesAsync();

                if (isOrg != null)
                {
                    EmailService.SendAccountInfoEmail(isOrg.Email!, user.UserName, randomPassword);
                }
                else if (isVolunteer != null)
                {
                    EmailService.SendAccountInfoEmail(isVolunteer.Email!, user.UserName, randomPassword);
                }
                TempData["SuccessMessage"] = "Đã chấp thuận cấp quyền thành công! Vui lòng kiểm tra Email";

                return Json(new { success = true, message = "Event accepted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            #endregion
        }

    }
}
