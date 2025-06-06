﻿using System;
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
            var today = DateOnly.FromDateTime(DateTime.Now); // 04:19 PM +07, May 27, 2025

            // 1. Lấy dữ liệu cho "Our Causes" (các sự kiện có TargetFunds > 0)
            var causes = _context.Events
                .Include(e => e.Donations)
                .Where(e => e.TargetFunds > 0)
                .OrderByDescending(e => e.DayBegin)
                .Take(6)
                .ToList();

            ViewBag.Causes = causes ?? new List<Event>();

            // 2. Lấy dữ liệu cho "Our Latest Events" (sự kiện sắp diễn ra, 3 sự kiện mới nhất)
            var latestEvents = _context.Events
                .Where(e => e.DayBegin > today)
                .OrderBy(e => e.DayBegin)
                .Take(3)
                .ToList();

            ViewBag.LatestEvents = latestEvents ?? new List<Event>();

            // 3. Lấy dữ liệu cho "Latest Donations" (kết hợp Donations và Registrations)
            var latestDonations = _context.Donations
               .Include(d => d.Volunteer)
               .Include(d => d.Event)
               .Select(d => new
               {
                   Type = "Donation",
                   Name = d.Volunteer != null ? d.Volunteer.Name ?? "Ẩn danh" : "Ẩn danh",
                   Amount = d.Amount,
                   EventName = d.Event != null ? d.Event.Name ?? "Không xác định" : "Không xác định",
                   ActionTime = d.DonationDate ?? DateTime.Now,
                   ImagePath = d.Volunteer != null && !string.IsNullOrEmpty(d.Volunteer.ImagePath)
                       ? d.Volunteer.ImagePath
                       : "~/images/DefaultImg/default-person.jpg"
               })
               .ToList();

            var latestRegistrations = _context.Registrations
                           .Include(r => r.Volunteer)
                           .Include(r => r.Event)
                           .Where(r => r.Status == "Được duyệt")
                           .Select(r => new
                           {
                               Type = "Volunteer",
                               Name = r.Volunteer != null ? r.Volunteer.Name ?? "Ẩn danh" : "Ẩn danh",
                               Amount = (decimal?)null,
                               EventName = r.Event != null ? r.Event.Name ?? "Không xác định" : "Không xác định",
                               ActionTime = r.RegisterAt != null ? r.RegisterAt.Value.ToDateTime(new TimeOnly(0, 0)) : DateTime.Now,
                               ImagePath = r.Volunteer != null && !string.IsNullOrEmpty(r.Volunteer.ImagePath)
                                   ? r.Volunteer.ImagePath
                                   : "~/images/default-person.jpg"
                           })
                           .ToList();

            // Combine and cast to a common type
            var latestActions = latestDonations
                .Cast<object>()
                .Concat(latestRegistrations.Cast<object>())
                .OrderByDescending(a => ((dynamic)a).ActionTime)
                .Take(3)
                .ToList();

            // Assign to ViewBag
            ViewBag.LatestDonations = latestActions.Any() ? latestActions : new List<object>();

            return View();
        }
     

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpGet]
        public IActionResult Events(int page = 1, int pageSize = 6, string statusFilter = "ongoing", string eventName = "", string organization = "", string startDateFrom = "", string startDateTo = "", string eventType = "")
        {
            var query = _context.Events
                .Include(e => e.Org)
                .Include(e => e.TypeEvent)
                .Include(e => e.Registrations)
                .Include(e => e.Donations)
                .Where(e => e.Status == "ACCEPTED") 
                .AsQueryable();
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "ongoing")
                {
                    query = query.Where(e => e.DayBegin <= today && e.DayEnd >= today);
                }
                else if (statusFilter == "upcoming")
                {
                    query = query.Where(e => e.DayBegin > today);
                }
                else if (statusFilter == "ended")
                {
                    query = query.Where(e => e.DayEnd < today);
                }
            }

            if (!string.IsNullOrEmpty(eventName))
            {
                query = query.Where(e => e.Name != null && e.Name.Contains(eventName));
            }

            if (!string.IsNullOrEmpty(organization))
            {
                query = query.Where(e => e.Org != null && e.Org.Name != null && e.Org.Name.Contains(organization));
            }

            if (!string.IsNullOrEmpty(startDateFrom))
            {
                var startDate = DateOnly.Parse(startDateFrom);
                query = query.Where(e => e.DayBegin >= startDate || e.DayEnd >= startDate);
            }

            if (!string.IsNullOrEmpty(startDateTo))
            {
                var endDate = DateOnly.Parse(startDateTo);
                query = query.Where(e => e.DayBegin <= endDate || e.DayEnd <= endDate);
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(e => e.TypeEvent != null && e.TypeEvent.Name == eventType);
            }

            var totalEvents = query.Count();

            var totalPages = (int)Math.Ceiling((double)totalEvents / pageSize);

            page = Math.Max(1, page);
            page = Math.Min(page, totalPages > 0 ? totalPages : 1);
            var eventList = query
                .OrderByDescending(e => e.DayBegin)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.EventName = eventName;
            ViewBag.Organization = organization;
            ViewBag.StartDateFrom = startDateFrom;
            ViewBag.StartDateTo = startDateTo;
            ViewBag.EventType = eventType;

            return View(eventList);
        }

        [HttpGet]
        public IActionResult Detail_Event(string id)
        {
            var eventDetail = _context.Events
                .Include(e => e.Org)
                .Include(e => e.Donations)
                .FirstOrDefault(e => e.EventId == id);

            if (eventDetail == null)
            {
                return NotFound();
            }

           
            var eventDetailWithOrg = (from e in _context.Events
                                      join org in _context.Organizations
                                      on e.OrgId equals org.OrgId
                                      where e.EventId == id
                                      select new
                                      {
                                          Event = e,
                                          Organization = org
                                      }).FirstOrDefault();

            if (eventDetailWithOrg == null)
            {
                return NotFound();
            }

            var donationDetails = (from v in _context.Volunteers
                                   join d in _context.Donations on v.VolunteerId equals d.VolunteerId
                                   where d.EventId == id
                                   orderby d.DonationDate descending
                                   select new
                                   {
                                       VolunteerName = v.Name ?? "Unknown",
                                       Volunteer_Id = v.VolunteerId,
                                       EventId = d.EventId,
                                       Amount = d.Amount,
                                       DonationDate = d.DonationDate
                                   }).ToList();

         
            ViewBag.Event = eventDetailWithOrg.Event;
            ViewBag.Organization = eventDetailWithOrg.Organization;
            ViewBag.Donations = donationDetails;

            return View(eventDetailWithOrg.Event);
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
        #region Danh sách tình nguyện viên
        public IActionResult Volunteer_List()
        {
            // Eagerly load Volunteers with related Donations and Registrations
            var volunteers = _context.Volunteers
                .Include(v => v.Donations)
                .Include(v => v.Registrations)
                .ToList(); // Materialize the query early

            // Join with Users to get CreateAt (JoinDate)
            var volunteerData = volunteers
                .GroupJoin(_context.Users.AsEnumerable(),
                    v => v.VolunteerId,
                    u => u.UserId,
                    (v, users) => new
                    {
                        Volunteer = v,
                        User = users.FirstOrDefault()
                    })
                .Select(vu => new
                {
                    Volunteer = vu.Volunteer,
                    JoinDate = vu.User != null ? vu.User.CreateAt : null,
                    TotalDonations = vu.Volunteer.Donations.Sum(d => d.Amount ?? 0),
                    EventCount = vu.Volunteer.Registrations.Count()
                })
                .ToList();

            // Store additional data in ViewBag
            ViewBag.VolunteerInfo = volunteerData.ToDictionary(
                v => v.Volunteer.VolunteerId,
                v => new
                {
                    JoinDate = v.JoinDate != null ? v.JoinDate.Value.ToString("dd/MM/yyyy") : "N/A",
                    TotalDonations = v.TotalDonations.ToString("C"),
                    EventCount = v.EventCount.ToString()
                });

            // Pass only the Volunteer objects to the view
            return View(volunteers);
        }
        #endregion

        [HttpGet]
        public IActionResult GetAcceptedEvents()
        {
            var acceptedEvents = _context.Events
                .Where(e => e.Status != null && e.Status.Equals("ACCEPT", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return View(acceptedEvents);
        }

    }
}
