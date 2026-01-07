using Microsoft.AspNetCore.Mvc;
using PrintServer.Models;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly IFilePreviewService _previewService;
    private readonly ILogger<UploadController> _logger;
    private readonly string _uploadFolder;

    public UploadController(
        IUploadService uploadService,
        IFilePreviewService previewService,
        ILogger<UploadController> logger,
        IConfiguration configuration)
    {
        _uploadService = uploadService;
        _previewService = previewService;
        _logger = logger;
        _uploadFolder = configuration["UploadFolder"] ?? "uploads";
    }

    [HttpPost]
    public async Task<ActionResult<UploadResponse>> Upload([FromForm] IFormFile file)
    {
        try
        {
            _logger.LogInformation($"收到上传请求，文件: {file?.FileName}, 大小: {file?.Length ?? 0}");
            
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("文件为空或未接收到文件");
                return BadRequest(new { error = "未接收到文件或文件为空" });
            }

            var (fileId, fileName) = await _uploadService.SaveFileAsync(file);
            _logger.LogInformation($"文件上传成功: {fileName}");
            
            // 生成文件预览
            var filePath = Path.Combine(_uploadFolder, Path.GetFileName(fileId));
            var preview = await _previewService.GeneratePreviewAsync(filePath);
            
            return Ok(new UploadResponse
            {
                FileId = fileId,
                FileName = fileName,
                FileSize = file.Length,
                FileType = Path.GetExtension(fileName),
                Preview = preview
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    // 批量上传
    [HttpPost("batch")]
    public async Task<ActionResult<List<UploadResponse>>> UploadBatch([FromForm] List<IFormFile> files)
    {
        try
        {
            _logger.LogInformation($"收到批量上传请求，文件数: {files?.Count ?? 0}");
            
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { error = "未接收到文件" });
            }

            var responses = new List<UploadResponse>();
            
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var (fileId, fileName) = await _uploadService.SaveFileAsync(file);
                    var filePath = Path.Combine(_uploadFolder, Path.GetFileName(fileId));
                    var preview = await _previewService.GeneratePreviewAsync(filePath);
                    
                    responses.Add(new UploadResponse
                    {
                        FileId = fileId,
                        FileName = fileName,
                        FileSize = file.Length,
                        FileType = Path.GetExtension(fileName),
                        Preview = preview
                    });
                }
            }
            
            _logger.LogInformation($"批量上传成功: {responses.Count} 个文件");
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量上传失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    // 获取文件预览
    [HttpGet("preview/{fileId}")]
    public async Task<ActionResult<FilePreview>> GetPreview(string fileId)
    {
        try
        {
            var filePath = Path.Combine(_uploadFolder, Path.GetFileName(fileId));
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "文件不存在" });
            }

            var preview = await _previewService.GeneratePreviewAsync(filePath);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件预览失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}
