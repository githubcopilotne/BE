using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace BE.Helpers;

public class VnPayHelper
{
    private readonly IConfiguration _config;

    public VnPayHelper(IConfiguration config)
    {
        _config = config;
    }

   
    // Tạo URL thanh toán VNPay
    public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string ipAddress)
    {
        var tmnCode = _config["VnPay:TmnCode"]!;
        var hashSecret = _config["VnPay:HashSecret"]!;
        var payUrl = _config["VnPay:PayUrl"]!;
        var returnUrl = _config["VnPay:ReturnUrl"]!;

        // Bước 1: Gom các param
        var vnpParams = new SortedDictionary<string, string>
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", tmnCode },
            { "vnp_Amount", ((long)(amount * 100)).ToString() },  // Nhân 100 theo yêu cầu VNPay
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", ipAddress },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_TxnRef", orderId.ToString() },
            { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") },
        };

        // Bước 2: Nối params thành chuỗi (đã sắp xếp A-Z nhờ SortedDictionary)
        var queryString = new StringBuilder();
        var hashData = new StringBuilder();

        foreach (var (key, value) in vnpParams)
        {
            queryString.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
            hashData.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
        }

        // Xóa dấu & cuối
        queryString.Length--;
        hashData.Length--;

        // Bước 3: Tạo chữ ký HMAC-SHA512
        var secureHash = HmacSha512(hashSecret, hashData.ToString());

        // Bước 4: Ghép thành URL hoàn chỉnh
        return $"{payUrl}?{queryString}&vnp_SecureHash={secureHash}";
    }

    
    // Verify chữ ký VNPay trả về (chống giả mạo)
    public bool ValidateSignature(IQueryCollection queryParams)
    {
        var hashSecret = _config["VnPay:HashSecret"]!;
        var vnpSecureHash = queryParams["vnp_SecureHash"].ToString();

        // Lấy tất cả params vnp_ (trừ vnp_SecureHash) → sắp xếp A-Z
        var sortedParams = new SortedDictionary<string, string>();
        foreach (var (key, value) in queryParams)
        {
            if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
            {
                sortedParams[key] = value.ToString();
            }
        }

        // Nối thành chuỗi (URL encode) → HMAC-SHA512
        var hashData = new StringBuilder();
        foreach (var (key, value) in sortedParams)
        {
            hashData.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
        }
        hashData.Length--; // Xóa & cuối

        var checkHash = HmacSha512(hashSecret, hashData.ToString());

        // So sánh chữ ký: khớp = thật, không khớp = giả mạo
        return checkHash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Mã hóa HMAC-SHA512
    /// </summary>
    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
