using BE.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsForClient(
            [FromQuery] int page = 1,
            [FromQuery] int size = 12,
            [FromQuery] string? category = null,
            [FromQuery] string sort = "newest",
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? search = null)
        {
            var result = await _productService.GetProductsForClient(page, size, category, sort, minPrice, maxPrice, search);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
