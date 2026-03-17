using BE.DTOs;
using BE.DTOs.Cart;

namespace BE.Services.Interfaces
{
    public interface ICartService
    {
        Task<ApiResponse<object>> GetCart(int userId);
        Task<ApiResponse<object>> AddToCart(int userId, AddToCartRequest request);
        Task<ApiResponse<object>> UpdateCartItem(int userId, UpdateCartItemRequest request);
        Task<ApiResponse<object>> RemoveFromCart(int userId, int variantId);
        Task<ApiResponse<object>> SyncCart(int userId, SyncCartRequest request);
    }
}
