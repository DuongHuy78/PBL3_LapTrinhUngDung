using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Volunteer_website.Helpers;
using Volunteer_website.Models;
using Microsoft.AspNetCore.Authorization;


public class OrganizationController : Controller
{
    private readonly VolunteerManagementContext _db;

    public OrganizationController(VolunteerManagementContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    #region Register Organization
    [HttpGet]
    public IActionResult RegisterOrganization()
    {
        return View(new Organization());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterOrganization(
        [FromForm] Organization organization,
        IFormFile? organizationImg)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin: " + string.Join("; ", errors);
            return View(organization);
        }

        // Check for duplicate organization name
        if (await _db.Organizations.AnyAsync(c =>
            c.Name.ToLower() == organization.Name.ToLower()))
        {
            TempData["Error"] = "Tên tổ chức đã tồn tại";
            return View(organization);
        }

        try
        {
            // Generate new OrgId
            var lastOrg = await _db.Organizations
                .OrderByDescending(o => o.OrgId)
                .FirstOrDefaultAsync();

            string nextOrgId = "ORG0001";
            if (lastOrg != null)
            {
                if (int.TryParse(lastOrg.OrgId.Substring(3), out int lastId))
                {
                    nextOrgId = $"ORG{(lastId + 1):D4}";
                }
                else
                {
                    throw new InvalidOperationException("Invalid OrgId format in database");
                }
            }
            organization.OrgId = nextOrgId;

            // Handle image upload
            if (organizationImg != null && organizationImg.Length > 0)
            {
                try
                {
                    organization.ImagePath = await UpLoadImgService.UploadImg(
                        organizationImg,
                        "OrgsImg");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tải lên hình ảnh: " + ex.Message;
                    return View(organization);
                }
            }

            // Add and save organization
            await _db.Organizations.AddAsync(organization);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tổ chức thành công";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Đã xảy ra lỗi khi đăng ký tổ chức: " + ex.Message;
            return View(organization);
        }
    }
    #endregion
}