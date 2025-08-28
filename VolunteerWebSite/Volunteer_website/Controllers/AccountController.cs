using Microsoft.AspNetCore.Mvc;
using Volunteer_website.Models;
using AutoMapper;
using Volunteer_website.Helpers;
using MyCommerce.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.ViewModels;
using AspNetCoreGeneratedDocument;

namespace Volunteer_website.Controllers
{
    public class AccountController : Controller
    {
        private readonly VolunteerManagementContext _db;
        private readonly IMapper _mapper;

        public AccountController(VolunteerManagementContext context, IMapper mapper)
        {
            this._db = context;
            _mapper = mapper;
        }

        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterVM();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data! Please check the form and try again.";
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                return View(model);
            }

            // Kiểm tra username đã tồn tại
            if (_db.Users.Any(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác!");
                return View(model);
            }

            // Kiểm tra định dạng và trùng lặp PhoneNumber
            if (!InputValidator.IsValidPhoneNumber(model.PhoneNumber!))
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại không phù hợp. Vui lòng nhập số điện thoại khác!");
                return View(model);
            }

            if (await _db.Volunteers.AnyAsync(v => v.PhoneNumber == model.PhoneNumber) ||
                await _db.Organizations.AnyAsync(v => v.PhoneNumber == model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã tồn tại. Vui lòng nhập số điện thoại khác!");
                return View(model);
            }

            // Kiểm tra định dạng và trùng lặp Email
            if (!InputValidator.IsValidEmail(model.Email!))
            {
                ModelState.AddModelError("Email", "Định dạng Email không hợp lệ. Vui lòng nhập lại!");
                return View(model);
            }

            if (await _db.Volunteers.AnyAsync(v => v.Email == model.Email) ||
                await _db.Organizations.AnyAsync(v => v.Email == model.Email) ||
                await _db.Admins.AnyAsync(v => v.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại. Vui lòng nhập Email khác!");
                return View(model);
            }

            // Kiểm tra DateOfBirth
            if (model.DateOfBirth == null)
            {
                ModelState.AddModelError("DateOfBirth", "Ngày sinh không được để trống!");
                return View(model);
            }
            if (DateOnly.FromDateTime((DateTime)model.DateOfBirth) > DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("DateOfBirth", "Ngày sinh không được trong tương lai!");
                return View(model);
            }

            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    var user = new User();

                    // Tạo UserId với tiền tố "VOL"
                    var lastUser = await _db.Volunteers
                        .OrderByDescending(u => u.VolunteerId)
                        .FirstOrDefaultAsync();
                    if (lastUser == null)
                    {
                        user.UserId = "VOL0001";
                    }
                    else
                    {
                        // Trích xuất phần số từ "VOLXXXX" (ví dụ: "VOL0007" -> "0007")
                        string numericPart = lastUser.VolunteerId.Substring(3); // Lấy "0007"
                        if (int.TryParse(numericPart, out int temp))
                        {
                            temp++;
                            user.UserId = $"VOL{temp:D4}";
                        }
                        else
                        {
                            throw new FormatException($"Invalid numeric part in UserId: {lastUser.VolunteerId}");
                        }
                    }

                    user.RandomKey = Util.GenerateRandomkey();
                    user.UserName = model.UserName;
                    user.Password = model.Password.ToMd5Hash(user.RandomKey);
                    user.Role = 0;
                    user.IsActive = true;
                    user.CreateAt = DateOnly.FromDateTime(DateTime.Now);

                    var volunteer = new Volunteer
                    {
                        VolunteerId = user.UserId,
                        PhoneNumber = model.PhoneNumber,
                        Email = model.Email,
                        Name = model.Name,
                        DateOfBirth = DateOnly.FromDateTime((DateTime)model.DateOfBirth!),
                        Gender = model.Gender,
                        ImagePath = model.ImagePath,
                        Address = model.Address
                    };

                    _db.Users.Add(user);
                    _db.Volunteers.Add(volunteer);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đăng ký thành công";
                    return RedirectToAction("Index", "Home");
                }
                catch (FormatException ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Lỗi: {ex.Message}.";
                    return View(model);
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Database error occurred during registration. Please try again later.";
                    Console.WriteLine($"DbUpdateException: {ex.InnerException?.Message ?? ex.Message}");
                    return View(model);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An unexpected error occurred during registration. Please try again.";
                    Console.WriteLine($"Exception: {ex.Message}");
                    return View(model);
                }
            }
        }
        #endregion

        #region Login
        [HttpGet]
        public ActionResult Login(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Login(LoginVM model, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                ModelState.Values.SelectMany(v => v.Errors).ToList().ForEach(error =>
                    Console.WriteLine("Error ModelState: " + error.ErrorMessage));
                return View(model);
            }

            var user = _db.Users.SingleOrDefault(kh => kh.UserName == model.UserName);
            if (user == null)
            {
                ModelState.AddModelError("UserName", "Không tìm thấy người dùng.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("UserName", "Tài khoản đã bị khóa. Vui lòng liên hệ Admin qua Email.");
                return View(model);
            }

            if (user.Password != model.Password.ToMd5Hash(user.RandomKey))
            {
                ModelState.AddModelError("Password", "Sai thông tin đăng nhập");
                return View(model);
            }

            if (user.Role == 0)
            {
                var volunteer = _db.Volunteers.SingleOrDefault(vol => vol.VolunteerId == user.UserId);
                if (volunteer == null)
                {
                    ModelState.AddModelError("UserName", "Thông tin tình nguyện viên không tồn tại.");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, volunteer.VolunteerId),
                    new Claim(ClaimTypes.Name, volunteer.Name ?? ""),
                    new Claim(ClaimTypes.Email, volunteer.Email ?? ""),
                    new Claim(ClaimTypes.MobilePhone, volunteer.PhoneNumber ?? ""),
                    new Claim(ClaimTypes.StreetAddress, volunteer.Address ?? ""),
                    new Claim(ClaimTypes.Gender, volunteer.Gender == true? "Male" : "Female"),
                    new Claim(ClaimTypes.Role, user.Role.ToString()!),
                     new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

             
                    new Claim("ImagePath", volunteer.ImagePath ?? "default.jpg")
                    
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                try
                {
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
                    TempData["SuccessMessage"] = "You've successfully logged in!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi SignInAsync: " + ex.Message);
                    return View(model);
                }

                // Role 0: Điều hướng về Index của Home Controller
                return (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    ? Redirect(returnUrl)
                    : RedirectToAction("Index", "Home");
            }
            else if (user.Role == 1)
            {
                var org = _db.Organizations.SingleOrDefault(vol => vol.OrgId == user.UserId);
                if (org == null)
                {
                    ModelState.AddModelError("loi", "Thông tin tổ chức không tồn tại.");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, org.Name ?? ""),
                    new Claim(ClaimTypes.Email, org.Email ?? ""),
                    new Claim(ClaimTypes.MobilePhone, org.PhoneNumber ?? ""),
                    new Claim(ClaimTypes.StreetAddress, org.Address ?? ""),
                    new Claim(ClaimTypes.Gender, org.Description ?? ""),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                try
                {
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
                    Console.WriteLine("Đăng nhập thành công");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi SignInAsync: " + ex.Message);
                    return View(model);
                }

                return (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    ? Redirect(returnUrl)
                    : Redirect(Url.Action("Index", "Statistics", new { area = "Organization" })!);
            }
            else if (user.Role == 2)
            {
                var Admin = _db.Admins.SingleOrDefault(vol => vol.AdminId == user.UserId);
                if (Admin == null)
                {
                    ModelState.AddModelError("loi", "Thông tin Admin không tồn tại.");
                    return View(model);
                }
                var ImgPath = Admin.ImgPath;
                if (ImgPath == null) ImgPath = "/images/default.jpg";
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Admin.Name ?? ""),
                    new Claim(ClaimTypes.Email, Admin.Email ?? ""),
                    new Claim(ClaimTypes.Role, user.Role.ToString()!),
                    new Claim("AvatarUrl", ImgPath),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                try
                {
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
                    Console.WriteLine("Đăng nhập thành công");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi SignInAsync: " + ex.Message);
                    return View(model);
                }

                return (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    ? Redirect(returnUrl)
                    : Redirect(Url.Action("Index", "HomeAdmin", new { area = "Admin" })!);
            }


            // Nếu không thuộc role 0 hoặc 1, có thể thêm điều hướng mặc định hoặc thông báo lỗi
            ModelState.AddModelError("loi", "Vai trò không hợp lệ.");
            return View(model);
        }


        #endregion

        #region forgot password
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            var model = new ForgotPasswordVM();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if(!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Something wrong please check again.";
                return View(model);
            }
            var currentUser = _db.Users.FirstOrDefault(u => u.UserName == model.UserName);

            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                RedirectToAction("Login", "Home");
            }

            if(_db.Volunteers.Any(v => v.VolunteerId == currentUser!.UserId)) {
                var temp = _db.Volunteers.FirstOrDefault(v => v.VolunteerId == currentUser!.UserId);
                if(temp!.Email != model.Email)
                {
                    TempData["ErrorMessage"] = "The email associated with your account is different from the one you entered.";
                    return View(model);
                }
            }
            else if (_db.Organizations.Any(v => v.OrgId == currentUser!.UserId))
            {
                var temp = _db.Organizations.FirstOrDefault(v => v.OrgId == currentUser!.UserId);
                if (temp!.Email != model.Email)
                {
                    TempData["ErrorMessage"] = "The email associated with your account is different from the one you entered.";
                    return View(model);
                }
            }
            else if (_db.Admins.Any(v => v.AdminId == currentUser!.UserId))
            {
                var temp = _db.Admins.FirstOrDefault(v => v.AdminId == currentUser!.UserId);
                if (temp!.Email != model.Email)
                {
                    TempData["ErrorMessage"] = "The email associated with your account is different from the one you entered.";
                    return View(model);
                }
            }

            try
            {
                var randomPassword = Util.GenerateRandomkey(6);
                currentUser.RandomKey = Util.GenerateRandomkey();
                currentUser.Password = randomPassword.ToMd5Hash(currentUser.RandomKey);
                Helpers.EmailService.SendNewPasswordEmail(model.Email, randomPassword);
                _db.Users.Update(currentUser);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Send Email Success! Please check your Email";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex;
                return View(model);
            }
        }
        #endregion

        #region Chỉnh sửa trang cá nhân
        [Authorize]
        public IActionResult profile()
        {

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chủ
        }
        #endregion
    }
}