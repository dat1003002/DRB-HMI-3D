using Microsoft.AspNetCore.Mvc;
using DRB_HMI_3D.Models;
using DRB_HMI_3D.Services;

namespace DRB_HMI_3D.Controllers
{
    [Route("Setting/Workshop")]
    public class WorkshopController : Controller
    {
        private readonly IWorkshopService _service;

        public WorkshopController(IWorkshopService service)
        {
            _service = service;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Setting/Workshop.cshtml");
        }

        [HttpGet("GetAll")]
        public async Task<JsonResult> GetAll()
        {
            var workshops = await _service.GetAllAsync();
            return Json(workshops);
        }

        [HttpGet("Get/{id}")]
        public async Task<JsonResult> Get(int id)
        {
            var workshop = await _service.GetByIdAsync(id);
            return Json(workshop);
        }

        [HttpPost("Create")]
        public async Task<JsonResult> Create([FromBody] Workshop workshop)
        {
            try
            {
                var result = await _service.CreateAsync(workshop);
                return Json(new { success = true, data = result, message = "Workshop created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Update")]
        public async Task<JsonResult> Update([FromBody] Workshop workshop)
        {
            try
            {
                await _service.UpdateAsync(workshop);
                return Json(new { success = true, message = "Workshop updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Json(new { success = true, message = "Workshop deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}