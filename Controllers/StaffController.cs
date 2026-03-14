using BE.DTOs.Staff;
using BE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 0,
            [FromQuery] int size = 15,
            [FromQuery] string? keyword = null,
            [FromQuery] int? status = null,
            [FromQuery] string? role = null)
        {
            var result = await _staffService.GetAll(page, size, keyword, status, role);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _staffService.GetById(id);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStaffRequest request)
        {
            var result = await _staffService.Create(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateStaffRequest request)
        {
            var result = await _staffService.Update(id, request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _staffService.ToggleStatus(id);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPatch("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var result = await _staffService.ResetPassword(id);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
