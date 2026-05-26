using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DRB_HMI_3D.Data;
using DRB_HMI_3D.Models;
using DRB_HMI_3D.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DRB_HMI_3D.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly HmiRealtimeStore _realtimeStore;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context,
            HmiRealtimeStore realtimeStore)
        {
            _logger = logger;
            _context = context;
            _realtimeStore = realtimeStore;
        }

        public async Task<IActionResult> Index(int? workshopId)
        {
            var workshop = await GetWorkshopAsync(workshopId);

            if (workshop == null)
            {
                return View(null);
            }

            return View(workshop);
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult GetRealtimeData(int? workshopId)
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            if (!workshopId.HasValue)
            {
                return Json(new
                {
                    success = false,
                    message = "Thiếu workshopId."
                });
            }

            if (_realtimeStore.TryGetWorkshopData(workshopId.Value, out var data))
            {
                return Json(data);
            }

            return Json(new
            {
                success = false,
                message = "Chưa có dữ liệu realtime."
            });
        }

        private async Task<Workshop?> GetWorkshopAsync(int? workshopId)
        {
            var query = _context.Workshops
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.PressGroups)
                    .ThenInclude(g => g.PressItems)
                .AsQueryable();

            if (workshopId.HasValue)
            {
                return await query
                    .FirstOrDefaultAsync(w => w.Id == workshopId.Value);
            }

            return await query
                .OrderBy(w => w.Id)
                .FirstOrDefaultAsync();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}