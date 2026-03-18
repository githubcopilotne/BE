using BE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        public class ValidateVoucherRequest
        {
            public string VoucherCode { get; set; } = null!;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateVoucher(ValidateVoucherRequest request)
        {
            var result = await _voucherService.ValidateVoucher(request.VoucherCode);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }

  
}
