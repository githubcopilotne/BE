using BE.DTOs;
using BE.Models;
using BE.Services.Interfaces;
using System.Text.Json;

namespace BE.Services.Implementations
{
    public class GhnService : IGhnService
    {
        private readonly HttpClient _httpClient;
        private readonly string _masterDataUrl;
        private readonly string _apiUrl;
        private readonly string _shopId;

        public GhnService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Token", configuration["GHN:Token"]);
            _masterDataUrl = configuration["GHN:MasterDataUrl"]!;
            _apiUrl = configuration["GHN:ApiUrl"]!;
            _shopId = configuration["GHN:ShopId"]!.ToString();
        }

        public async Task<ApiResponse<object>> GetProvinces()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_masterDataUrl}/master-data/province");
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                var data = json.GetProperty("data");
                var provinces = data.EnumerateArray().Select(p => new
                {
                    provinceId = p.GetProperty("ProvinceID").GetInt32(),
                    provinceName = p.GetProperty("ProvinceName").GetString()
                })
                .OrderBy(p => p.provinceName)
                .ToList();

                return ApiResponse<object>.SuccessResponse(provinces, "Lấy danh sách tỉnh/thành phố thành công");
            }
            catch
            {
                return ApiResponse<object>.ErrorResponse("Không thể kết nối dịch vụ GHN");
            }
        }

        public async Task<ApiResponse<object>> GetDistricts(int provinceId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_masterDataUrl}/master-data/district",
                    new { province_id = provinceId }
                );
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                var data = json.GetProperty("data");
                var districts = data.EnumerateArray().Select(d => new
                {
                    districtId = d.GetProperty("DistrictID").GetInt32(),
                    districtName = d.GetProperty("DistrictName").GetString()
                })
                .OrderBy(d => d.districtName)
                .ToList();

                return ApiResponse<object>.SuccessResponse(districts, "Lấy danh sách quận/huyện thành công");
            }
            catch
            {
                return ApiResponse<object>.ErrorResponse("Không thể kết nối dịch vụ GHN");
            }
        }

        public async Task<ApiResponse<object>> GetWards(int districtId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_masterDataUrl}/master-data/ward",
                    new { district_id = districtId }
                );
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                var data = json.GetProperty("data");
                var wards = data.EnumerateArray().Select(w => new
                {
                    wardCode = w.GetProperty("WardCode").GetString(),
                    wardName = w.GetProperty("WardName").GetString()
                })
                .OrderBy(w => w.wardName)
                .ToList();

                return ApiResponse<object>.SuccessResponse(wards, "Lấy danh sách phường/xã thành công");
            }
            catch
            {
                return ApiResponse<object>.ErrorResponse("Không thể kết nối dịch vụ GHN");
            }
        }

        // Tính phí vận chuyển qua GHN API
        // Dùng HttpRequestMessage thay vì PostAsJsonAsync vì cần thêm header ShopId riêng cho request này
        public async Task<ApiResponse<object>> CalculateShippingFee(int districtId, string wardCode, int weight, int insuranceValue)
        {
            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/shipping-order/fee")
                {
                    Content = JsonContent.Create(new
                    {
                        service_type_id = 2, // E-Commerce Delivery
                        to_district_id = districtId,
                        to_ward_code = wardCode,
                        weight = weight,
                        insurance_value = insuranceValue
                    })
                };
                httpRequest.Headers.Add("ShopId", _shopId);

                var response = await _httpClient.SendAsync(httpRequest);
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                var data = json.GetProperty("data");
                var total = data.GetProperty("total").GetInt32();

                return ApiResponse<object>.SuccessResponse(
                    new { shippingFee = total },
                    "Tính phí vận chuyển thành công"
                );
            }
            catch
            {
                return ApiResponse<object>.ErrorResponse("Không thể tính phí vận chuyển");
            }
        }
        // Tạo đơn vận chuyển trên GHN
        public async Task<ApiResponse<object>> CreateShippingOrder(Order order)
        {
            try
            {
                var orderItems = order.OrderItems.ToList();
                // Tính tổng cân nặng
                var totalWeight = orderItems.Sum(i => i.Variant.Product.Weight * i.Quantity);

                // COD: GHN thu tiền hộ = totalMoney. Online: đã thanh toán → 0
                var codAmount = order.PaymentMethod == 0 ? (int)order.TotalMoney : 0;

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/shipping-order/create")
                {
                    Content = JsonContent.Create(new
                    {
                        payment_type_id = 1,             // Shop trả phí ship cho GHN
                        required_note = "CHOXEMHANGKHONGTHU", // Cho xem hàng, không cho thử
                        to_name = order.FullName,
                        to_phone = order.Phone,
                        to_address = order.Address,
                        to_ward_code = order.WardCode,
                        to_district_id = order.DistrictId,
                        cod_amount = codAmount,
                        weight = totalWeight,
                        service_type_id = 2,             // E-Commerce Delivery
                        items = orderItems.Select(i => new
                        {
                            name = i.Variant.Product.ProductName,
                            quantity = i.Quantity,
                            price = (int)i.Price,
                            weight = i.Variant.Product.Weight * i.Quantity
                        }).ToList()
                    })
                };
                httpRequest.Headers.Add("ShopId", _shopId);

                var response = await _httpClient.SendAsync(httpRequest);
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (!response.IsSuccessStatusCode)
                {
                    var message = json.GetProperty("message").GetString();
                    return ApiResponse<object>.ErrorResponse($"GHN: {message}");
                }

                var data = json.GetProperty("data");
                var orderCode = data.GetProperty("order_code").GetString();

                return ApiResponse<object>.SuccessResponse(
                    new { orderCode },
                    "Tạo đơn vận chuyển thành công"
                );
            }
            catch
            {
                return ApiResponse<object>.ErrorResponse("Không thể tạo đơn vận chuyển trên GHN");
            }
        }
    }
}
