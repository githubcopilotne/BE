using BE.DTOs;
using BE.Services.Interfaces;
using System.Text.Json;

namespace BE.Services.Implementations
{
    public class GhnService : IGhnService
    {
        private readonly HttpClient _httpClient;
        private readonly string _masterDataUrl;

        public GhnService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Token", configuration["GHN:Token"]);
            _masterDataUrl = configuration["GHN:MasterDataUrl"]!;
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
    }
}
