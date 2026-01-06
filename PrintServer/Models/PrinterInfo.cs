namespace PrintServer.Models;

/// <summary>
/// 打印机详细信息
/// </summary>
public class PrinterInfo
{
    /// <summary>
    /// 打印机名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 打印机状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 是否为默认打印机
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// 支持彩色打印
    /// </summary>
    public bool SupportsColor { get; set; }

    /// <summary>
    /// 支持双面打印
    /// </summary>
    public bool SupportsDuplex { get; set; }

    /// <summary>
    /// 支持的纸张大小
    /// </summary>
    public List<string> SupportedPaperSizes { get; set; } = new();

    /// <summary>
    /// 打印机类型
    /// </summary>
    public string PrinterType { get; set; } = string.Empty;

    /// <summary>
    /// 端口名称
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// 驱动名称
    /// </summary>
    public string DriverName { get; set; } = string.Empty;
}
