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
    }
}
