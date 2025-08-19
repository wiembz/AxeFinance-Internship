using Microsoft.AspNetCore.Http;

namespace TechDashboardAPI.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
    Task<string> SaveFileAsync(IFormFile file, string subfolder = "");
    Task<bool> DeleteFileAsync(string filePath);
    Task<Stream> GetFileAsync(string filePath);
    Task<byte[]> GetFileBytesAsync(string filePath);
    string GetFileUrl(string filePath);
    bool FileExists(string filePath);
    bool IsValidFileType(string fileName, string[] allowedExtensions);
    bool IsValidFileSize(long fileSize, long maxSizeInMb = 10);
}
