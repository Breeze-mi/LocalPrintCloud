namespace PrintServer.Services;

public class UploadService : IUploadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UploadService> _logger;
    private readonly string _uploadFolder;
    private readonly string[] _allowedExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx", ".txt", ".doc", ".xls" };

    public UploadService(IConfiguration configuration, ILogger<UploadService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _uploadFolder = _configuration["UploadFolder"] ?? "uploads";
        
        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }
    }

    public async Task<(string fileId, string fileName)> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("文件为空");
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new NotSupportedException($"不支持的文件格式: {extension}");
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation($"文件已保存: {fileName}");
        return (Path.Combine(_uploadFolder, fileName), file.FileName);
    }

    public string GetFilePath(string fileId)
    {
        return Path.GetFullPath(fileId);
    }
}
