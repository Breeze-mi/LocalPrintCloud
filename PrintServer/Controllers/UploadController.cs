using Microsoft.AspNetCore.Mvc;
using PrintServer.Models;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IUploadService uploadService, ILogger<UploadController> logger)
    {
        _uploadService = uploadService;
        _logger = logger;
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
            
            return Ok(new UploadResponse
            {
                FileId = fileId,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}
