using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Converters;
using Volunteer_website.ViewModels;
using Volunteer_website.Models;
using Volunteer_website.Services;
namespace Volunteer_website.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly VolunteerManagementContext _context;
        private readonly IVnPayService _vnPayService;
        public HomeController(ILogger<HomeController> logger, VolunteerManagementContext context, IVnPayService vnPayService)
        {
            _logger = logger;
            _context = context;
            _vnPayService = vnPayService;
        }

        public IActionResult Index()
        {
            if(User.IsInRole("2"))
            {
                return RedirectToAction("Index", "HomeAdmin", new { area = "Admin"});
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Events()
        {

            var eventList = _context.Events
                .OrderByDescending(e => e.DayBegin)
                .Select(e => new EventModel
                {
                    Event_Id = e.EventId,
                    Name = e.Name ?? "Không có tên",
                    EventDescription = e.Description ?? "Không có mô tả",
                    ImagePath = e.ImagePath ?? "/images/default-event.jpg", // Sử dụng ImagePath mới
                    DateBegin = e.DayBegin ?? DateOnly.FromDateTime(DateTime.Today),
                    DateEnd = e.DayEnd ?? DateOnly.FromDateTime(DateTime.Today),
                    Location = e.Location ?? "N/A",
                    Organization = e.Org != null ? e.Org.Name ?? "N/A" : "N/A",
                    targetmember = e.TargetMember ?? 0,
                    targetfund = e.TargetFunds.HasValue ? (int)e.TargetFunds.Value : 0,
                    currentmember = e.Registrations.Count(ev => ev.Status == "Được duyệt"),
                    currentfund = e.Donations != null ? (int)e.Donations.Sum(d => d.Amount ?? 0) : 0,
                    type = e.TypeEvent != null ? e.TypeEvent.TypeEventId : "N/A"
                })
                .ToList();

            return View(eventList);
        }
        

        [HttpGet]
        public IActionResult Detail_Event(string id)
        {
            var eventDetail = _context.Events.FirstOrDefault(e => e.EventId == id);
            if (eventDetail == null)
            {
                return NotFound();
            }

            var event_detail = (from v in _context.Events
                                where v.EventId == id
                                join ev in _context.Organizations
                                on v.OrgId equals ev.OrgId
                                //join e in _context.EventImages
                                //on v.EventId equals e.EventId
                                join d in _context.Donations
                                on v.EventId equals d.EventId into donations
                                select new EventModel
                                {
                                    Event_Id = v.EventId,
                                    Name = v.Name,
                                    EventDescription = v.Description,
                                    ImagePath = v.ImagePath,
                                    DateBegin = v.DayBegin ?? DateOnly.FromDateTime(DateTime.Today),
                                    DateEnd = v.DayEnd ?? DateOnly.FromDateTime(DateTime.Today),
                                    Location = v.Location,
                                    Organization = ev.Name,
                                    OrganizationImagePath = ev.ImagePath,
                                    OrganizationDescription = ev.Description,
                                    OrganizationPhone = ev.PhoneNumber,
                                    OrganizationEmail = ev.Email,
                                    targetmember = v.TargetMember ?? 0,
                                    targetfund = v.TargetFunds.HasValue ? (int)v.TargetFunds.Value : 0,
                                    currentmember = v.Donations.Count(d=>d.EventId==id),
                                    currentfund = v.Donations != null ? (int)v.Donations.Sum(d => d.Amount ?? 0) : 0
                                    
                                }).FirstOrDefault();
            var donationDetails = (from v in _context.Volunteers
                                    join d in _context.Donations on v.VolunteerId equals d.VolunteerId
                                    where d.EventId == id
                                    orderby d.DonationDate descending 
                                    select new Donate_List
                                    {
                                        VolunteerName = v.Name ?? "Unknown",
                                        Volunteer_Id = v.VolunteerId,
                                        EventId = d.EventId,
                                        Amount = d.Amount,
                                        DonationDate = d.DonationDate
                                    }).ToList();

            if (event_detail != null)
            {
                event_detail.Donations = donationDetails;
            }

            return View(event_detail);
        }
        

        public IActionResult Fundraising()
        {
            return View();
        }
        public IActionResult Volunteers()
        {
            return View();
        }
        public IActionResult Blogs()
        {
            return View();
        }
        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Volunteer()
        {
            return View();
        }
        public IActionResult IndexUser()
        {
            return View();
        }
        public IActionResult Volunteer_List()
        {
            var volunteers = (from v in _context.Volunteers
                              join ev in _context.Registrations
                              on v.VolunteerId equals ev.VolunteerId
                              where ev.Status == "Đã tham gia"
                              select new Volunteer_List
                              {
                                  VolunteerId = v.VolunteerId,
                                  Name = v.Name,
                                  Email = v.Email,
                                  PhoneNumber = v.PhoneNumber,
                                  JoinDate = ev.RegisterAt
                              })
                              .Distinct()
                              .ToList();

            return View(volunteers);
        }
       
    }
}
