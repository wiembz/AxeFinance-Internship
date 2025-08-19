using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TechDashboardAPI.Application.Interfaces;

namespace TechDashboardAPI.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadsPath;
    private readonly IWebHostEnvironment _env;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly long _maxFileSizeBytes; // e.g., 10MB default
    private readonly string[] _defaultAllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt", ".zip" };
    private readonly HashSet<string> _allowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif",
        "application/pdf",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "application/zip", "application/x-zip-compressed"
    };

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _env = env;
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            // Fallback to content root if WebRootPath is not set
            webRoot = _env.ContentRootPath;
        }
        var root = Path.Combine(webRoot, "uploads");
        _uploadsPath = Path.GetFullPath(root);
        _baseUrl = configuration["BaseUrl"] ?? "https://localhost:7077";
        var maxMb = 10L;
        if (long.TryParse(configuration["FileUpload:MaxFileSizeMB"], out var configuredMb) && configuredMb > 0)
        {
            maxMb = configuredMb;
        }
        _maxFileSizeBytes = maxMb * 1024 * 1024;
        
        // Ensure uploads directory exists
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            if (!IsAllowedContentType(contentType))
                throw new ArgumentException($"Invalid content type: {contentType}");

            var sanitizedName = SanitizeFileName(fileName);
            var extension = Path.GetExtension(sanitizedName);
            if (!IsValidFileType(sanitizedName, _defaultAllowedExtensions))
                throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", _defaultAllowedExtensions)}");

            var subfolder = GetSubfolderByContentType(contentType);
            var folderPath = SafeCombine(_uploadsPath, subfolder);
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedName}";
            var filePath = SafeCombine(folderPath, uniqueFileName);
            
            using var fileStreamOutput = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await fileStream.CopyToAsync(fileStreamOutput);
            
            var relativePath = Path.Combine(subfolder, uniqueFileName).Replace("\\", "/");
            _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subfolder = "")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        if (!IsValidFileType(file.FileName, _defaultAllowedExtensions))
            throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", _defaultAllowedExtensions)}");

        if (!IsAllowedContentType(file.ContentType))
            throw new ArgumentException($"Invalid content type: {file.ContentType}");

        if (!IsValidFileSize(file.Length))
            throw new ArgumentException($"File size exceeds maximum limit of {_maxFileSizeBytes / (1024 * 1024)} MB");

        try
        {
            var sanitizedName = SanitizeFileName(file.FileName);
            var targetFolder = string.IsNullOrEmpty(subfolder) ? "general" : SanitizePathSegment(subfolder);
            var folderPath = SafeCombine(_uploadsPath, targetFolder);
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedName}";
            var filePath = SafeCombine(folderPath, uniqueFileName);
            
            using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fileStream);
            
            var relativePath = Path.Combine(targetFolder, uniqueFileName).Replace("\\", "/");
            _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var normalizedRel = NormalizeRelativePath(filePath);
            var fullPath = SafeCombine(_uploadsPath, normalizedRel);
            
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }
            
            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<Stream> GetFileAsync(string filePath)
    {
        try
        {
            var normalizedRel = NormalizeRelativePath(filePath);
            var fullPath = SafeCombine(_uploadsPath, normalizedRel);
            
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {filePath}");
            
            return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file stream: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<byte[]> GetFileBytesAsync(string filePath)
    {
        try
        {
            var normalizedRel = NormalizeRelativePath(filePath);
            var fullPath = SafeCombine(_uploadsPath, normalizedRel);
            
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {filePath}");
            
            return await File.ReadAllBytesAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file bytes: {FilePath}", filePath);
            throw;
        }
    }

    public string GetFileUrl(string filePath)
    {
    var rel = NormalizeRelativePath(filePath).Replace("\\", "/");
    return $"{_baseUrl}/uploads/{rel}";
    }

    public bool FileExists(string filePath)
    {
        try
        {
            var normalizedRel = NormalizeRelativePath(filePath);
            var fullPath = SafeCombine(_uploadsPath, normalizedRel);
            return File.Exists(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
            return false;
        }
    }

    public bool IsValidFileType(string fileName, string[] allowedExtensions)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsValidFileSize(long fileSize, long maxSizeInMb = 10)
    {
        long limit = _maxFileSizeBytes > 0 ? _maxFileSizeBytes : maxSizeInMb * 1024 * 1024;
        return fileSize <= limit;
    }

    private static string GetSubfolderByContentType(string contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            var ct when ct?.StartsWith("image/") == true => "images",
            var ct when ct?.Contains("pdf") == true => "documents",
            var ct when ct?.Contains("word") == true => "documents",
            var ct when ct?.Contains("excel") == true => "documents",
            _ => "general"
        };
    }

    private bool IsAllowedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        return _allowedMimeTypes.Contains(contentType);
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "file";
        var name = Path.GetFileName(fileName); // strips any path
        // Replace invalid chars with '_'
        var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var regex = new Regex($"[{invalid}]", RegexOptions.Compiled);
        name = regex.Replace(name, "_");
        // Limit length
        if (name.Length > 120)
        {
            var ext = Path.GetExtension(name);
            var baseName = Path.GetFileNameWithoutExtension(name);
            baseName = baseName[..Math.Min(baseName.Length, 100)];
            name = baseName + ext;
        }
        return name;
    }

    private static string SanitizePathSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment)) return string.Empty;
        segment = segment.Replace("\\", "/");
        segment = segment.Trim('/', ' ');
        // Disallow traversal
        if (segment.Contains("..")) throw new ArgumentException("Invalid path segment.");
        // Keep only safe chars
        segment = Regex.Replace(segment, "[^a-zA-Z0-9_-]", "");
        return segment;
    }

    private static string NormalizeRelativePath(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative)) return string.Empty;
        relative = relative.Replace("\\", "/");
        relative = relative.TrimStart('/');
        if (relative.Contains("..")) throw new ArgumentException("Invalid path.");
        return relative;
    }

    private static string SafeCombine(string root, string path)
    {
        var combined = Path.GetFullPath(Path.Combine(root, path));
        if (!combined.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Attempted path traversal outside of uploads directory.");
        return combined;
    }
}
