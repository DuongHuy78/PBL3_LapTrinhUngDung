using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.Helpers;
using Volunteer_website.Models;
using X.PagedList.Extensions;
using MyCommerce.Models;
using X.PagedList;
using Volunteer_website.Areas.Admins.Data;

namespace Volunteer_website.Areas.Admins.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class OrgManagerController : Controller
    {
        private readonly VolunteerManagementContext _context;

        public OrgManagerController(VolunteerManagementContext context)
        {
            _context = context;
        }

        #region Index
        [HttpGet]
        public IActionResult Index(int page = 1)
        {
            int pageSize = 8;
            int pageNumber = page;

            var numberEvents = _context.Events
                .GroupBy(org => org.OrgId)
                .Select(g => new { orgId = g.Key, eventCount = g.Count() })
                .ToDictionary(x => x.orgId!, x => x.eventCount);

            ViewBag.NumberEvents = numberEvents;

            var lstOrg = _context.Organizations.AsNoTracking()
                            .OrderBy(x => x.OrgId)
                            .ToPagedList(pageNumber, pageSize);
            return View(lstOrg);
        }
        #endregion

        #region Duyệt tổ chức
        [HttpGet]
        public async Task<IActionResult> ApprovalOrg(int page = 1)
        {
            int pageSize = 8;
            int pageNumber = page;

            // 1. Tạo truy vấn cơ bản (không thực thi)
            var query = _context.Organizations
                    .AsNoTracking()
                    .Where(org => !_context.Users.Any(user => user.UserId == org.OrgId))
                    .OrderBy(x => x.OrgId);

            // 2. Đếm tổng số bản ghi
            int totalCount = await query.CountAsync();

            // 3. Lấy dữ liệu cho trang hiện tại
            var organizationsWithoutUsers = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            // 4. Tạo StaticPagedList
            var pagedResults = new StaticPagedList<Models.Organization>(
                organizationsWithoutUsers, pageNumber, pageSize, totalCount);

            return View(pagedResults);
        }

        [HttpPost]
        public async Task<IActionResult> RejectApprovalOrg(string id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Lỗi dữ liệu đầu vào không phù hợp.";
                return RedirectToAction("ApprovalOrg");
            }

            var currentOrg = await _context.Organizations.FirstOrDefaultAsync(m => m.OrgId == id);
            if (currentOrg == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tổ chức.";
                return RedirectToAction("ApprovalOrg");
            }

            _context.Organizations.Remove(currentOrg);
            await _context.SaveChangesAsync();
            return RedirectToAction("ApprovalOrg");
        }

        [HttpGet]
        public async Task<IActionResult> AcceptApprovalOrg(string id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Lỗi dữ liệu đầu vào không phù hợp.";
                return RedirectToAction("ApprovalOrg");
            }

            var currentOrg = await _context.Organizations.FirstOrDefaultAsync(m => m.OrgId == id);
            if (currentOrg == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tổ chức.";
                return RedirectToAction("ApprovalOrg");
            }

            if (currentOrg.Email == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy email của tổ chức.";
                return RedirectToAction("ApprovalOrg");
            }
            // Kiểm tra xem user đã tồn tại chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (existingUser == null)
            {
                // Tạo mật khẩu ngẫu nhiên
                var randomPassword = Util.GenerateRandomkey(6);

                // Tạo tài khoản mới
                var user = new User();
                user.UserId = id;
                user.RandomKey = Util.GenerateRandomkey();
                user.UserName = Util.GenerateRandomkey(6);
                user.Password = randomPassword.ToMd5Hash(user.RandomKey);
                user.Role = 1;
                user.IsActive = true;
                user.CreateAt = DateOnly.FromDateTime(DateTime.Now);  // Ngày tạo tài khoản

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Gửi thông báo thành công
                TempData["SuccessMessage"] = "Đã chấp thuận tổ chức và tạo tài khoản thành công!";
                EmailService.SendAccountInfoEmail(currentOrg.Email, user.UserName, randomPassword);
                return RedirectToAction("ApprovalOrg");
            }
            else
            {
                // Nếu tài khoản đã tồn tại, chỉ cần kích hoạt
                existingUser.IsActive = true;
                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã kích hoạt tài khoản tổ chức thành công!";
            }

            return RedirectToAction("ApprovalOrg");
        }
        #endregion

        #region danh sách sự kiện
        public IActionResult ListEvent(string id, int? page)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;

            // Lấy danh sách event của tổ chức
            var events = _context.Events
                .Where(e => e.OrgId == id)
                .OrderBy(e => e.EventId)
                .Select(e => new ListEvent
                {
                    EventId = e.EventId,
                    OrgId = e.OrgId,
                    TypeEvent = e.TypeEvent!.Name,
                    Name = e.Name,
                    Description = e.Description,
                    DayBegin = e.DayBegin,
                    DayEnd = e.DayEnd,
                    Location = e.Location,
                    TargetMember = e.TargetMember,
                    TargetFunds = e.TargetFunds,
                    IsActive = e.IsActive,
                    Status = e.Status,
                    // Tính CurrentMember và CurrentFunds nếu cần
                    CurrentMember = e.Registrations != null ? e.Registrations.Count : 0,
                    CurrentFunds = e.Donations != null ? e.Donations.Sum(d => d.Amount) : 0
                })
                .ToPagedList(pageNumber, pageSize);

            ViewData["id"] = id;

            return View(events);
        }
        #endregion

        #region
        #endregion
    }
}
