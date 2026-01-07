using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using PrintClient.Avalonia.Models;
using PrintClient.Avalonia.Services;
using ReactiveUI;

namespace PrintClient.Avalonia.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private Timer? _refreshTimer;

    [ObservableProperty]
    private string _serverUrl = "http://10.22.19.132:5000";

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
    private int _pageRangeMode = 0;

    [ObservableProperty]
    private string _customPageRange = string.Empty;

    [ObservableProperty]
    private bool _isCustomPageRange = false;

    [ObservableProperty]
    private int _duplexMode = 0;

    [ObservableProperty]
    private int _colorMode = 1;

    [ObservableProperty]
    private int _paperSizeMode = 0;

    [ObservableProperty]
    private int _orientationMode = 0;

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

    // ReactiveUI Commands
    public ReactiveCommand<Unit, Unit> GetPrintersCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; }
    public ReactiveCommand<Unit, Unit> UploadAndPrintCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshJobsCommand { get; }

    public MainViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        UpdateApiBaseUrl();

        // 初始化命令
        GetPrintersCommand = ReactiveCommand.CreateFromTask(GetPrintersAsync);
        SelectFileCommand = ReactiveCommand.Create(SelectFile);
        UploadAndPrintCommand = ReactiveCommand.CreateFromTask(UploadAndPrintAsync);
        RefreshJobsCommand = ReactiveCommand.CreateFromTask(RefreshJobsAsync);

        // 延迟启动自动刷新
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            StartAutoRefresh();
        });
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

    private async Task GetPrintersAsync()
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
        }
    }

    private void SelectFile()
    {
        // 文件选择将在 View 中处理
        StatusMessage = "请选择文件...";
    }

    public void OnFileSelected(string filePath)
    {
        SelectedFilePath = filePath;
        SelectedFileName = Path.GetFileName(filePath);
        StatusMessage = $"已选择: {SelectedFileName}";
    }

    private async Task UploadAndPrintAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            StatusMessage = "请先选择文件";
            return;
        }

        if (string.IsNullOrEmpty(SelectedPrinter))
        {
            StatusMessage = "请先选择打印机";
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
                    Quality = 1,
                    Scale = ScalePercent,
                    PageRange = PageRangeMode == 1 ? CustomPageRange : null,
                    FitToPage = FitToPage,
                    AutoRotateAndCenter = AutoRotateAndCenter,
                    PagesPerSheet = 1
                }
            };

            var printResponse = await _apiClient.PrintAsync(printRequest);

            StatusMessage = $"打印任务已提交: {printResponse.JobId}";
            await RefreshJobsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败: {ex.Message}";
        }
    }

    private async Task RefreshJobsAsync()
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
            else
            {
                StatusMessage = "暂无打印任务";
            }
        }
        catch
        {
            if (Jobs.Count == 0)
            {
                StatusMessage = "无法连接到服务器，请检查服务端是否运行";
            }
        }
    }

    private void StartAutoRefresh()
    {
        // 首次加载
        Task.Run(async () =>
        {
            await Task.Delay(500);
            await RefreshJobsAsync();
        });

        // 定时刷新
        _refreshTimer = new Timer(10000);
        _refreshTimer.Elapsed += async (s, e) =>
        {
            try
            {
                await RefreshJobsAsync();
            }
            catch
            {
                // 忽略错误
            }
        };
        _refreshTimer.Start();
    }
}
