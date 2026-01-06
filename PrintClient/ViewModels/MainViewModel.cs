using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PrintClient.Models;
using PrintClient.Services;

namespace PrintClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private string _serverUrl = "http://localhost:5000";

    [ObservableProperty]
    private ObservableCollection<string> _printers = new();

    [ObservableProperty]
    private string? _selectedPrinter;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private string _selectedFileName = "未选择文件";

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private int _pageRangeMode = 0;  // 0=全部, 1=自定义

    [ObservableProperty]
    private string _customPageRange = string.Empty;

    [ObservableProperty]
    private bool _isCustomPageRange = false;

    [ObservableProperty]
    private int _duplexMode = 0;  // 0=单面, 1=双面长边, 2=双面短边

    [ObservableProperty]
    private int _colorMode = 1;  // 0=黑白, 1=彩色

    [ObservableProperty]
    private int _paperSizeMode = 0;  // 0=A4, 1=A3, 2=A5, 3=Letter, 4=Legal

    [ObservableProperty]
    private int _orientationMode = 0;  // 0=纵向, 1=横向

    [ObservableProperty]
    private int _scalePercent = 100;

    [ObservableProperty]
    private bool _fitToPage = true;

    [ObservableProperty]
    private bool _autoRotateAndCenter = true;

    partial void OnPageRangeModeChanged(int value)
    {
        IsCustomPageRange = value == 1;
    }

    [ObservableProperty]
    private ObservableCollection<PrintJob> _jobs = new();

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public MainViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        UpdateApiBaseUrl();
        StartAutoRefresh();
    }

    partial void OnServerUrlChanged(string value)
    {
        UpdateApiBaseUrl();
    }

    private void UpdateApiBaseUrl()
    {
        try
        {
            _apiClient.SetBaseUrl(ServerUrl);
        }
        catch (Exception ex)
        {
            StatusMessage = $"无效的服务器地址: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GetPrinters()
    {
        try
        {
            StatusMessage = "正在获取打印机列表...";
            var printers = await _apiClient.GetPrintersAsync();
            Printers.Clear();
            foreach (var printer in printers)
            {
                Printers.Add(printer);
            }
            
            if (Printers.Count > 0 && SelectedPrinter == null)
            {
                SelectedPrinter = Printers[0];
            }
            
            StatusMessage = $"已获取 {Printers.Count} 台打印机";
        }
        catch (Exception ex)
        {
            StatusMessage = $"获取打印机失败: {ex.Message}";
            MessageBox.Show($"获取打印机列表失败:\n{ex.Message}", "错误", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SelectFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "所有支持的文件|*.pdf;*.png;*.jpg;*.jpeg;*.docx;*.xlsx;*.txt|" +
                     "PDF 文件|*.pdf|" +
                     "图片文件|*.png;*.jpg;*.jpeg|" +
                     "Word 文档|*.docx|" +
                     "Excel 表格|*.xlsx|" +
                     "文本文件|*.txt|" +
                     "所有文件|*.*",
            Title = "选择要打印的文件"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            SelectedFileName = Path.GetFileName(dialog.FileName);
            StatusMessage = $"已选择: {SelectedFileName}";
        }
    }

    [RelayCommand]
    private async Task UploadAndPrint()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            MessageBox.Show("请先选择文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(SelectedPrinter))
        {
            MessageBox.Show("请先选择打印机", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            StatusMessage = "正在上传文件...";
            var uploadResponse = await _apiClient.UploadFileAsync(SelectedFilePath);
            
            StatusMessage = "正在发起打印...";
            var printRequest = new PrintRequest
            {
                FileId = uploadResponse.FileId,
                PrinterName = SelectedPrinter,
                Copies = Copies,
                Options = new PrintOptions 
                { 
                    Duplex = DuplexMode,
                    Color = ColorMode,
                    PaperSize = PaperSizeMode,
                    Orientation = OrientationMode,
                    Quality = 1,  // 标准质量
                    Scale = ScalePercent,
                    PageRange = PageRangeMode == 1 ? CustomPageRange : null,
                    FitToPage = FitToPage,
                    AutoRotateAndCenter = AutoRotateAndCenter,
                    PagesPerSheet = 1
                }
            };

            var printResponse = await _apiClient.PrintAsync(printRequest);
            
            StatusMessage = $"打印任务已提交: {printResponse.JobId}";
            MessageBox.Show($"打印任务已提交!\n任务ID: {printResponse.JobId}", "成功", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            
            await RefreshJobs();
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败: {ex.Message}";
            MessageBox.Show($"上传或打印失败:\n{ex.Message}", "错误", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task RefreshJobs()
    {
        try
        {
            var jobs = await _apiClient.GetAllJobsAsync();
            Jobs.Clear();
            foreach (var job in jobs)
            {
                Jobs.Add(job);
            }
            if (Jobs.Count > 0)
            {
                StatusMessage = $"已刷新任务列表 ({Jobs.Count} 个任务)";
            }
        }
        catch
        {
            // 静默失败，不显示错误
        }
    }

    private void StartAutoRefresh()
    {
        _refreshTimer = new System.Timers.Timer(10000); // 每 10 秒刷新一次
        _refreshTimer.Elapsed += async (s, e) =>
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // 只有在有任务时才自动刷新
                    if (Jobs.Count > 0)
                    {
                        await RefreshJobs();
                    }
                });
            }
            catch
            {
                // 忽略自动刷新错误
            }
        };
        _refreshTimer.Start();
    }
}
