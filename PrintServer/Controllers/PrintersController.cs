using Microsoft.AspNetCore.Mvc;
using PrintServer.Services;
using PrintServer.Models;

namespace PrintServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintersController : ControllerBase
{
    private readonly IPrinterService _printerService;

    public PrintersController(IPrinterService printerService)
    {
        _printerService = printerService;
    }

    /// <summary>
    /// 获取打印机名称列表（简单）
    /// </summary>
    [HttpGet]
    public ActionResult<List<string>> GetPrinters()
    {
        var printers = _printerService.GetPrinters();
        return Ok(printers);
    }

    /// <summary>
    /// 获取打印机详细信息列表
    /// </summary>
    [HttpGet("detailed")]
    public ActionResult<List<PrinterInfo>> GetPrintersDetailed()
    {
        var printers = _printerService.GetPrintersDetailed();
        return Ok(printers);
    }
}
