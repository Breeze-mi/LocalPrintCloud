using PrintServer.Models;

namespace PrintServer.Services;

public interface IPrintQueueService
{
    void EnqueueJob(PrintJob job);
    Task StartProcessingAsync();
}
