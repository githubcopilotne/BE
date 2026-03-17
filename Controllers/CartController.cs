using BE.DTOs.Cart;
using BE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _cartService.GetCart(userId);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(AddToCartRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _cartService.AddToCart(userId, request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPatch("update")]
        public async Task<IActionResult> UpdateCartItem(UpdateCartItemRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _cartService.UpdateCartItem(userId, request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpDelete("remove/{variantId}")]
        public async Task<IActionResult> RemoveFromCart(int variantId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _cartService.RemoveFromCart(userId, variantId);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
