using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using BE.Services.Interfaces;

namespace BE.Services.Implementations
{
    // Service xử lý upload ảnh lên Cloudinary
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        // Đọc 3 key từ appsettings.json để khởi tạo kết nối Cloudinary
        public CloudinaryService(IConfiguration config)
        {
            var settings = config.GetSection("Cloudinary");
            var account = new Account(
                settings["CloudName"],
                settings["ApiKey"],
                settings["ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        // Upload 1 file ảnh lên Cloudinary, trả về URL
        public async Task<string> Upload(IFormFile file, string folder)
        {
            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception("Upload ảnh thất bại: " + result.Error.Message);

            return result.SecureUrl.ToString();
        }
    }
}
