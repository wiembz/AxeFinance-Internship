using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Application.DTOs;

namespace TechDashboardAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileStorageService fileStorageService, ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string? subfolder = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var filePath = await _fileStorageService.SaveFileAsync(file, subfolder ?? "general");
            var fileUrl = _fileStorageService.GetFileUrl(filePath);

            return Ok(new FileUploadResponseDto
            {
                FileName = file.FileName,
                FilePath = filePath,
                FileUrl = fileUrl,
                Size = file.Length,
                ContentType = file.ContentType
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultipleFiles(List<IFormFile> files, [FromQuery] string? subfolder = null)
    {
        try
        {
            if (files == null || !files.Any())
            {
                return BadRequest(new { message = "No files provided" });
            }

            var uploadResults = new List<FileUploadResponseDto>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = await _fileStorageService.SaveFileAsync(file, subfolder ?? "general");
                    var fileUrl = _fileStorageService.GetFileUrl(filePath);

                    uploadResults.Add(new FileUploadResponseDto
                    {
                        FileName = file.FileName,
                        FilePath = filePath,
                        FileUrl = fileUrl,
                        Size = file.Length,
                        ContentType = file.ContentType
                    });
                }
            }

            return Ok(new { files = uploadResults, count = uploadResults.Count });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return StatusCode(500, new { message = "An error occurred while uploading files" });
        }
    }

    [HttpGet("download/{*filePath}")]
    public async Task<IActionResult> DownloadFile(string filePath)
    {
        try
        {
            if (!_fileStorageService.FileExists(filePath))
            {
                return NotFound(new { message = "File not found" });
            }

            var fileBytes = await _fileStorageService.GetFileBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            var contentType = GetContentType(fileName);

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
            return StatusCode(500, new { message = "An error occurred while downloading the file" });
        }
    }

    [HttpDelete("delete/{*filePath}")]
    [Authorize(Policy = "ContributorOrAbove")]
    public async Task<IActionResult> DeleteFile(string filePath)
    {
        try
        {
            var result = await _fileStorageService.DeleteFileAsync(filePath);
            
            if (result)
            {
                return Ok(new { message = "File deleted successfully" });
            }
            
            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return StatusCode(500, new { message = "An error occurred while deleting the file" });
        }
    }

    [HttpGet("exists/{*filePath}")]
    public IActionResult FileExists(string filePath)
    {
        try
        {
            var exists = _fileStorageService.FileExists(filePath);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
            return StatusCode(500, new { message = "An error occurred while checking file existence" });
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
