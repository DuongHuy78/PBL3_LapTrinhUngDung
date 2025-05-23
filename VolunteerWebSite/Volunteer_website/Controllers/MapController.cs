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
            var data = _db.Events.Select(e => new
            {
                name = e.Name,
                address = e.Location // hoặc e.Address nếu tên cột là vậy
            }).ToList();

            return Json(data);
        }

    }
}
