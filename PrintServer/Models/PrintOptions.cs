namespace PrintServer.Models;

/// <summary>
/// 打印选项
/// </summary>
public class PrintOptions
{
    /// <summary>
    /// 双面打印模式
    /// </summary>
    public DuplexMode Duplex { get; set; } = DuplexMode.Simplex;

    /// <summary>
    /// 颜色模式
    /// </summary>
    public ColorMode Color { get; set; } = ColorMode.Color;

    /// <summary>
    /// 纸张大小
    /// </summary>
    public PaperSize PaperSize { get; set; } = PaperSize.A4;

    /// <summary>
    /// 打印方向
    /// </summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    /// <summary>
    /// 打印质量
    /// </summary>
    public PrintQuality Quality { get; set; } = PrintQuality.Normal;

    /// <summary>
    /// 页面范围 (例如: "1-5,8,11-13")
    /// </summary>
    public string? PageRange { get; set; }

    /// <summary>
    /// 缩放比例 (50-200)
    /// </summary>
    public int Scale { get; set; } = 100;

    /// <summary>
    /// 每页多版 (1, 2, 4, 6, 9, 16)
    /// </summary>
    public int PagesPerSheet { get; set; } = 1;

    /// <summary>
    /// 是否自动旋转和居中
    /// </summary>
    public bool AutoRotateAndCenter { get; set; } = true;

    /// <summary>
    /// 是否适应页面大小
    /// </summary>
    public bool FitToPage { get; set; } = false;

    /// <summary>
    /// 逐份打印（每份完整后再打印下一份）
    /// 重要：自助打印机的核心功能！
    /// true: 打印顺序为 1,2,3,1,2,3（推荐）
    /// false: 打印顺序为 1,1,2,2,3,3
    /// </summary>
    public bool Collate { get; set; } = true;

    /// <summary>
    /// 反片打印（从最后一页开始打印）
    /// </summary>
    public bool ReverseOrder { get; set; } = false;
}

/// <summary>
/// 双面打印模式
/// </summary>
public enum DuplexMode
{
    /// <summary>
    /// 单面打印
    /// </summary>
    Simplex = 0,

    /// <summary>
    /// 双面打印 - 长边翻转
    /// </summary>
    DuplexLongEdge = 1,

    /// <summary>
    /// 双面打印 - 短边翻转
    /// </summary>
    DuplexShortEdge = 2
}

/// <summary>
/// 颜色模式
/// </summary>
public enum ColorMode
{
    /// <summary>
    /// 黑白
    /// </summary>
    Monochrome = 0,

    /// <summary>
    /// 彩色
    /// </summary>
    Color = 1
}

/// <summary>
/// 纸张大小
/// </summary>
public enum PaperSize
{
    A4,
    A3,
    A5,
    Letter,
    Legal,
    Tabloid,
    Custom
}

/// <summary>
/// 页面方向
/// </summary>
public enum PageOrientation
{
    /// <summary>
    /// 纵向
    /// </summary>
    Portrait = 0,

    /// <summary>
    /// 横向
    /// </summary>
    Landscape = 1
}

/// <summary>
/// 打印质量
/// </summary>
public enum PrintQuality
{
    /// <summary>
    /// 草稿
    /// </summary>
    Draft = 0,

    /// <summary>
    /// 标准
    /// </summary>
    Normal = 1,

    /// <summary>
    /// 高质量
    /// </summary>
    High = 2
}
