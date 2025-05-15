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

namespace Volunteer_website.Controllers
{
    public class Account2controller : Controller
    {
        private readonly VolunteerManagementContext _context;

        public Account2controller(VolunteerManagementContext context)
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
            var volunteerId = HttpContext.Session.GetString("UserId");

            if (volunteerId == null)
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
                AvatarPath = volunteer.ImagePath,
                AvatarFile = null, 
                
            };
          
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Update_Infor(Update_ContactModel model, IFormFile avatarFile)
        {
            var volunteer = _context.Volunteers.FirstOrDefault(v => v.VolunteerId == model.VolunteerId);

            if (volunteer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tình nguyện viên.";
                return RedirectToAction("Update_Infor");
            }
       
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Kiểm tra kích thước file (5MB)
                if (avatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "File ảnh không được vượt quá 5MB");
                    return View(model);
                }

                // Kiểm tra loại file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(avatarFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (JPG, PNG, GIF)");
                    return View(model);
                }
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatarFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                volunteer.ImagePath = $"~/uploads/{fileName}";
            }

            volunteer.Name = model.Name;
            volunteer.Gender = model.Gender ?? false;
            volunteer.DateOfBirth = model.DateOfBirth;
            try
            {
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật: " + ex.Message;
            }
            return RedirectToAction("Update_Infor");
        }
        [HttpGet]
        public IActionResult Contact_Infor()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("SignIn", "Account");
            }      
            var volunteer = _context.Volunteers.FirstOrDefault(v => v.VolunteerId == userId);
            if (volunteer == null)
            {
                return NotFound(); // hoặc Redirect hoặc View thông báo lỗi
            }
            var model = new Update_ContactModel
            {

                Name = volunteer.Name,
                PhoneNumber = volunteer.PhoneNumber,
                Email = volunteer.Email,
                Address = volunteer.Address
            };

            return View(model);
        }



        [HttpPost]
        public IActionResult Contact_Infor(Update_ContactModel updatedVolunteer)
        {
            var addressErrors = ModelState["Address"]?.Errors;
            var allErrors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in allErrors)
            {
                Console.WriteLine("Lỗi: " + error.ErrorMessage);
            }

            foreach (var error in addressErrors)
            {
                Console.WriteLine("Lỗi Address: " + error.ErrorMessage);
            }

            if (ModelState.IsValid)
            {
                var VolunteerId = HttpContext.Session.GetString("UserId");

                if (string.IsNullOrEmpty(VolunteerId))
                {
                    return RedirectToAction("SignIn", "Account2");
                }

                try
                {
                    var volunteer = _context.Volunteers.FirstOrDefault(v => v.VolunteerId == VolunteerId);
                    if (volunteer == null)
                    {
                        return NotFound();
                    }

                    volunteer.Name = updatedVolunteer.Name;
                    volunteer.Email = updatedVolunteer.Email;
                    volunteer.PhoneNumber = updatedVolunteer.PhoneNumber;
                    volunteer.Address = updatedVolunteer.Address;

                    _context.Volunteers.Update(volunteer);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi khi cập nhật thông tin: " + ex.Message);
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật.";
                }

                return RedirectToAction("Contact_Infor", new { id = VolunteerId });
            }

            return View(updatedVolunteer);
        }

        private string GetLoggedInUserId()
        {
            return HttpContext.Session.GetString("VolunteerId");
        }

        public string HashPassword(string password)
        {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    return BitConverter.ToString(bytes).Replace("-", "").ToLower();
                }
         }

        [HttpGet]
        public IActionResult Change_PassWord()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Change_PassWord(Change_Password Temp)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.ErrorMessage = "Bạn chưa đăng nhập.";
                return RedirectToAction("SignIn", "Account2");
            }
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Không tìm thấy người dùng.";
                return View("Change_PassWord");
            }

            Console.WriteLine("UserId: " + userId); // Debugging line
            Console.WriteLine("OldPassword (entered): " + Temp.OldPassword); // Debugging line

            if (!Crypto.VerifyHashedPassword(user.Password, Temp.OldPassword))
            {
                ViewBag.ErrorMessage = "Mật khẩu cũ không đúng";
                return View("Change_PassWord");
            }

            if (Temp.NewPassword != Temp.ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp";
                return View("Change_PassWord");
            }

            user.Password = Crypto.HashPassword(Temp.NewPassword);
            _context.SaveChanges();
            ViewBag.Message = "Đổi mật khẩu thành công";

            return View("Change_PassWord");
        }


        [HttpGet]
        public IActionResult Sign_Up()
        {
            return View();
        }

        private string GenerateRandomKey()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Range(0, 8) 
                                      .Select(_ => chars[random.Next(chars.Length)])
                                      .ToArray());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sign_Up(SignUpModel su)
        {
            if (ModelState.IsValid)
            {
                if (_context.Volunteers.Any(u => u.Email == su.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    return View("Sign_Up");
                }
                if (_context.Users.Any(a => a.UserName == su.UserName))
                {
                    ModelState.AddModelError("UserName", "Tên người dùng đã tồn tại");
                    return View("Sign_Up");
                }
                string gen_key = GenerateRandomKey(); 
                string gen_id = gen_key + Guid.NewGuid().ToString(); 

                var account = new User
                {
                    UserId = gen_id,
                    UserName = su.UserName,
                    Password = Crypto.HashPassword(su.Password),
                    Role = 0,
                    CreateAt = DateOnly.FromDateTime(DateTime.Now),
                    RandomKey = gen_key
                };

                var volunteer = new Volunteer
                {
                    VolunteerId = gen_id,
                    PhoneNumber = su.PhoneNumber,
                    Email = su.Email,
                    Name = su.Name,
                    DateOfBirth = su.DateOfBirth.HasValue ? DateOnly.FromDateTime(su.DateOfBirth.Value) : (DateOnly?)null,
                    Gender = su.Gender,
                    Address = string.Empty, 
                    ImagePath = string.Empty, 
                };
                try
                {
                    _context.Users.Add(account);
                    _context.Volunteers.Add(volunteer);
                    _context.SaveChanges();
                    Console.WriteLine("Account ID: " + account.UserId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi khi lưu dữ liệu: " + ex.Message);
                    ModelState.AddModelError("", "Có lỗi khi lưu dữ liệu. Vui lòng thử lại.");
                    return View("Sign_Up");
                }

                return RedirectToAction("SignIn", "Account");
            }
            else
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ");
                return View(su);
            }
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignIn(SignInModel model)
        {

            if (ModelState.IsValid)
            {
                
                var account = _context.Users
                    .FirstOrDefault(a => a.UserName == model.UserName);

                if (account != null && Crypto.VerifyHashedPassword(account.Password, model.Password))
                {
                

                    var user = _context.Users.FirstOrDefault(u => u.UserId == account.UserId);
                    var vol = _context.Volunteers.FirstOrDefault(u => u.VolunteerId == account.UserId);
                    int role = user?.Role ?? 0; 

                    HttpContext.Session.SetString("UserId", account.UserId);
                    HttpContext.Session.SetInt32("UserRole", role);
                    HttpContext.Session.SetString("Name", account.UserName);
                    HttpContext.Session.SetString("Email", vol.Email);
                    return RedirectToAction("IndexUser", "Home");
                }
             
                
                    ModelState.AddModelError("", "Invalid username or password.");               
            }

            return View(model);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
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