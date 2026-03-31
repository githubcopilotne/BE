using BE.DTOs;
using BE.DTOs.Ghn;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GhnController : ControllerBase
    {
        private readonly IGhnService _ghnService;
        private readonly IOrderService _orderService;
        private readonly ShopQuanAoContext _context;

        public GhnController(IGhnService ghnService, IOrderService orderService, ShopQuanAoContext context)
        {
            _ghnService = ghnService;
            _orderService = orderService;
            _context = context;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var result = await _ghnService.GetProvinces();
            return Ok(result);
        }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var result = await _ghnService.GetDistricts(provinceId);
            return Ok(result);
        }

        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var result = await _ghnService.GetWards(districtId);
            return Ok(result);
        }

        [HttpPost("shipping-fee")]
        public async Task<IActionResult> CalculateShippingFee(ShippingFeeRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Query cart items từ DB → tính tổng weight + subtotal
            var cartItems = await _context.Carts
                .Where(c => c.UserId == userId)
                .SelectMany(c => c.CartItems)
                .Select(ci => new
                {
                    ci.Variant.Product.Weight,
                    ci.Variant.Product.UnitPrice,
                    ci.Quantity
                })
                .ToListAsync();

            if (!cartItems.Any())
                return Ok(ApiResponse<object>.ErrorResponse("Giỏ hàng trống"));

            var totalWeight = cartItems.Sum(i => i.Weight * i.Quantity);
            var subtotal = cartItems.Sum(i => (int)(i.UnitPrice * i.Quantity));

            var result = await _ghnService.CalculateShippingFee(
                request.DistrictId, request.WardCode, totalWeight, subtotal);
            return Ok(result);
        }

        // Webhook: GHN gọi vào khi trạng thái đơn thay đổi
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> GhnWebhook(GhnWebhookRequest request)
        {
            if (!string.IsNullOrEmpty(request.OrderCode))
                await _orderService.HandleGhnWebhook(request.OrderCode, request.Status);

            return Ok(); // GHN yêu cầu trả 200
        }
    }
}
