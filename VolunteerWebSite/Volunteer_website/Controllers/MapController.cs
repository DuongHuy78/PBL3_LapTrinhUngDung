using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Volunteer_website.Models;

namespace Volunteer_website.Controllers
{
    public class MapController : Controller
    {
        private readonly VolunteerManagementContext _db;
        public MapController(VolunteerManagementContext context, IMapper mapper)
        {
            this._db = context;
        }
        public IActionResult Index()
        {
           return View();
        }

       
        [HttpGet]
        public JsonResult GetEvents()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var data = _db.Events.Select(e => new
            {
                name = e.Name,
                address = e.Location, // Adjust to e.Address if that's the column name
                dayBegin = e.DayBegin, // Assuming DateTime or DateOnly
                dayEnd = e.DayEnd,     // Assuming DateTime or DateOnly
                status = e.DayBegin.HasValue && e.DayEnd.HasValue
                    ? (e.DayEnd.Value < today ? "Ended" : // Event ended
                       e.DayBegin.Value > today ? "Upcoming" : // Event hasn't started
                       "Ongoing") // Event is currently happening
                    : "Unknown" // Fallback for null dates
            }).ToList();

            return Json(data);
        }

    }
}
