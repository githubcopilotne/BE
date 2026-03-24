namespace BE.Services.Interfaces
{
    public interface ICloudinaryService
    {
        // Upload 1 file ảnh lên Cloudinary, trả về URL
        Task<string> Upload(IFormFile file, string folder);
    }
}
