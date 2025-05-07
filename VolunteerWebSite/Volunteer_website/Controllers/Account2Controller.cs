using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volunteer_website.Models;
using System.Web.Helpers;
using Volunteer_website.ViewModels;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        public IActionResult Update_Infor()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact_Infor()
        {       
            return View();
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
                    return RedirectToAction("SignIn", "Account");
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

                return RedirectToAction("Manage", new { id = VolunteerId });
            }

            return View(updatedVolunteer);
        }

        private string GetLoggedInUserId()
        {
            return HttpContext.Session.GetString("VolunteerId");
        }

        public IActionResult Change_Password()
        {
            return View();
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
                    int role = user?.Role ?? 0; // nếu không có thì mặc định luôn volunteer

                    HttpContext.Session.SetString("UserId", account.UserId.ToString());
                    HttpContext.Session.SetInt32("UserRole", role);
                    HttpContext.Session.SetString("Name", account.UserName);
                    //HttpContext.Session.SetString("Email", account);
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