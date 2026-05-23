using DRB_HMI_3D.Models;
using DRB_HMI_3D.Services;
using Microsoft.AspNetCore.Mvc;

namespace DRB_HMI_3D.Controllers
{
    public class PressItemTagController : Controller
    {
        private readonly IPressItemTagService _service;

        public PressItemTagController(IPressItemTagService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.PressGroups = await _service.GetPressGroupsAsync();
            var model = await _service.GetAllAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] PressItem model)
        {
            try
            {
                await _service.SaveAsync(model);
                return Json(new { success = true, message = "Lưu dữ liệu thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteItemAsync(id);
                return Json(new { success = true, message = "Xóa Press Item thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                await _service.DeleteTagAsync(id);
                return Json(new { success = true, message = "Xóa Press Tag thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}