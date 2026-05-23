using DRB_HMI_3D.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DRB_HMI_3D.Controllers
{
    public class PressController : Controller
    {
        private readonly AppDbContext _context;

        public PressController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? groupId)
        {
            ViewBag.GroupId = groupId;

            var workshopId = await GetWorkshopIdAsync(groupId);
            ViewBag.WorkshopId = workshopId;

            var pressItems = await _context.PressItems
                .AsNoTracking()
                .Where(x => x.Active && (groupId == null || x.PressGroupId == groupId.Value))
                .OrderBy(x => x.Id)
                .ToListAsync();

            return View(pressItems);
        }

        [HttpGet]
        public async Task<IActionResult> DebugMachines(int? groupId)
        {
            try
            {
                var data = await _context.PressItems
                    .AsNoTracking()
                    .Where(x => x.Active && (groupId == null || x.PressGroupId == groupId.Value))
                    .OrderBy(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.PressGroupId,
                        x.Active,
                        Tags = _context.PressTags
                            .AsNoTracking()
                            .Where(t => t.PressItemId == x.Id)
                            .Select(t => new
                            {
                                t.Id,
                                t.Name,
                                t.KepwareAddress
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Lỗi server",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        private async Task<int> GetWorkshopIdAsync(int? groupId)
        {
            if (groupId.HasValue)
            {
                var workshopId = await _context.PressGroups
                    .AsNoTracking()
                    .Where(x => x.Id == groupId.Value)
                    .Select(x => x.WorkshopId)
                    .FirstOrDefaultAsync();

                if (workshopId > 0)
                {
                    return workshopId;
                }
            }

            return await _context.Workshops
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
        }
    }
}