using BE.DTOs;
using BE.DTOs.Order;
using BE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _orderService.CreateOrder(userId, request);

            if (!result.Success)
                return BadRequest(result);

            // Nếu VNPay → tạo URL thanh toán
            if (request.PaymentMethod == 1)
            {
                var data = (dynamic)result.Data!;
                int orderId = data.orderId;
                decimal totalMoney = data.totalMoney;

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                var paymentUrl = _orderService.CreateVnPayUrl(orderId, totalMoney, ipAddress);

                return Ok(ApiResponse<object>.SuccessResponse(
                    new { orderId, paymentUrl },
                    "Đặt hàng thành công, vui lòng thanh toán"
                ));
            }

            return Ok(result);
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var result = await _orderService.VnPayReturn(Request.Query);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
