namespace PrintServer.Services;

public interface IUploadService
{
    Task<(string fileId, string fileName)> SaveFileAsync(IFormFile file);
    string GetFilePath(string fileId);
}
