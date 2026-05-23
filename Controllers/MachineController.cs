using Microsoft.AspNetCore.Mvc;
using DRB_HMI_3D.Models;
using DRB_HMI_3D.Services;

namespace DRB_HMI_3D.Controllers
{
    [Route("Setting/Machine")]
    public class MachineController : Controller
    {
        private readonly IPressGroupService _pressGroupService;

        public MachineController(IPressGroupService pressGroupService)
        {
            _pressGroupService = pressGroupService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int workshopId = 0)
        {
            var groups = await _pressGroupService.GetAllAsync(workshopId);
            ViewBag.WorkshopId = workshopId;
            return View("~/Views/Setting/Machine.cshtml", groups);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(int workshopId = 0)
        {
            var groups = await _pressGroupService.GetAllAsync(workshopId);
            return Ok(groups);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _pressGroupService.GetByIdAsync(id);

            if (group == null)
            {
                return NotFound(new { message = "Không tìm thấy Press Group" });
            }

            return Ok(group);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] PressGroup pressGroup)
        {
            if (pressGroup == null)
            {
                return BadRequest(new { message = "Dữ liệu gửi lên không hợp lệ" });
            }

            if (string.IsNullOrWhiteSpace(pressGroup.Label))
            {
                return BadRequest(new { message = "Label không được để trống" });
            }

            if (pressGroup.WorkshopId <= 0)
            {
                return BadRequest(new { message = "Workshop không hợp lệ" });
            }

            if (pressGroup.StartIndex < 0 || pressGroup.EndIndex < 0)
            {
                return BadRequest(new { message = "Start Index và End Index không được nhỏ hơn 0" });
            }

            if (pressGroup.StartIndex >= pressGroup.EndIndex)
            {
                return BadRequest(new { message = "Start Index phải nhỏ hơn End Index" });
            }

            var result = await _pressGroupService.CreateAsync(pressGroup);

            return Ok(result);
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] PressGroup pressGroup)
        {
            if (pressGroup == null)
            {
                return BadRequest(new { message = "Dữ liệu gửi lên không hợp lệ" });
            }

            if (pressGroup.Id <= 0)
            {
                return BadRequest(new { message = "Id không hợp lệ" });
            }

            if (string.IsNullOrWhiteSpace(pressGroup.Label))
            {
                return BadRequest(new { message = "Label không được để trống" });
            }

            if (pressGroup.WorkshopId <= 0)
            {
                return BadRequest(new { message = "Workshop không hợp lệ" });
            }

            if (pressGroup.StartIndex < 0 || pressGroup.EndIndex < 0)
            {
                return BadRequest(new { message = "Start Index và End Index không được nhỏ hơn 0" });
            }

            if (pressGroup.StartIndex >= pressGroup.EndIndex)
            {
                return BadRequest(new { message = "Start Index phải nhỏ hơn End Index" });
            }

            var oldGroup = await _pressGroupService.GetByIdAsync(pressGroup.Id);

            if (oldGroup == null)
            {
                return NotFound(new { message = "Không tìm thấy Press Group" });
            }

            oldGroup.WorkshopId = pressGroup.WorkshopId;
            oldGroup.Label = pressGroup.Label;
            oldGroup.StartIndex = pressGroup.StartIndex;
            oldGroup.EndIndex = pressGroup.EndIndex;
            oldGroup.Icon = pressGroup.Icon;

            var result = await _pressGroupService.UpdateAsync(oldGroup);

            return Ok(result);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _pressGroupService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new { message = "Không tìm thấy Press Group để xóa" });
            }

            return Ok(new { success = true });
        }
    }
}