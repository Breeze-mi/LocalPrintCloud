using System.Drawing.Printing;

namespace PrintServer.Services;

public interface IPrinterService
{
    List<string> GetPrinters();
    List<PrintServer.Models.PrinterInfo> GetPrintersDetailed();
    bool PrintFile(string filePath, string printerName, int copies, PrintServer.Models.PrintOptions? options = null);
    bool CancelPrintJob(string printerName, int jobId);
}
