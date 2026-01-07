using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PrintClient.Avalonia.ViewModels;

namespace PrintClient.Avalonia.Views;

public class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private TextBlock? _statusText;
    private ComboBox? _printerCombo;
    private TextBlock? _fileNameText;
    private NumericUpDown? _copiesInput;
    private ComboBox? _pageRangeCombo;
    private TextBox? _customPageRangeInput;
    private ComboBox? _duplexCombo;
    private ComboBox? _colorCombo;
    private ComboBox? _paperSizeCombo;
    private ComboBox? _orientationCombo;
    private Slider? _scaleSlider;
    private TextBlock? _scaleText;
    private CheckBox? _fitToPageCheck;
    private CheckBox? _autoRotateCheck;
    private DataGrid? _jobsGrid;

    public MainWindow()
    {
        Title = "网络打印客户端";
        Width = 1000;
        Height = 850;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#F5F5F5"));

        DataContextChanged += OnDataContextChanged;
        Opened += OnWindowOpened;
        
        Content = BuildUI();
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // 窗口打开后自动获取打印机列表
        if (_viewModel != null)
        {
            _viewModel.GetPrintersCommand.Execute().Subscribe();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as MainViewModel;
        BindData();
    }

    private Control BuildUI()
    {
        var mainGrid = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        // 标题栏
        var header = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2196F3")),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 20),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new TextBlock { Text = "🖨️", FontSize = 28, Margin = new Thickness(0, 0, 10, 0) },
                            new TextBlock { Text = "网络打印系统", FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Brushes.White }
                        }
                    },
                    (_statusText = new TextBlock
                    {
                        [Grid.ColumnProperty] = 1,
                        Text = "就绪",
                        Foreground = Brushes.White,
                        FontSize = 14
                    })
                }
            }
        };
        Grid.SetRow(header, 0);
        mainGrid.Children.Add(header);

        // 主内容 - 添加 ScrollViewer
        var scrollViewer = new ScrollViewer
        {
            [Grid.RowProperty] = 1,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var contentGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,3*")
        };

        // 左侧面板
        var leftPanel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        
        // 服务器设置
        var serverUrlInput = new TextBox
        {
            Text = "http://10.22.19.132:5000",
            FontSize = 14,
            Height = 42
        };
        leftPanel.Children.Add(CreateCard("🌐 服务器设置", serverUrlInput));

        // 打印机选择
        var printerPanel = new StackPanel();
        var printerHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 0, 0, 12)
        };
        printerHeader.Children.Add(new TextBlock { Text = "打印机", FontSize = 18, FontWeight = FontWeight.Bold });
        var refreshBtn = new Button
        {
            [Grid.ColumnProperty] = 1,
            Content = "刷新",
            Background = new SolidColorBrush(Color.Parse("#FF9800")),
            Foreground = Brushes.White,
            Padding = new Thickness(16, 8)
        };
        refreshBtn.Click += OnRefreshPrinters;
        printerHeader.Children.Add(refreshBtn);
        printerPanel.Children.Add(printerHeader);
        
        _printerCombo = new ComboBox { FontSize = 14, Height = 42 };
        printerPanel.Children.Add(_printerCombo);
        leftPanel.Children.Add(CreateCard("🖨️ 打印机", printerPanel));

        // 文件选择
        var filePanel = new StackPanel();
        var selectFileBtn = new Button
        {
            Content = "📁 选择文件",
            Background = new SolidColorBrush(Color.Parse("#9C27B0")),
            Foreground = Brushes.White,
            Height = 48,
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        selectFileBtn.Click += OnSelectFile;
        filePanel.Children.Add(selectFileBtn);
        _fileNameText = new TextBlock
        {
            Text = "未选择文件",
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#666666")),
            TextAlignment = TextAlignment.Center
        };
        filePanel.Children.Add(_fileNameText);
        leftPanel.Children.Add(CreateCard("📄 文件", filePanel));

        contentGrid.Children.Add(leftPanel);

        // 右侧面板
        var rightPanel = new StackPanel { [Grid.ColumnProperty] = 1, Margin = new Thickness(10, 0, 0, 0) };

        // 打印设置
        var settingsPanel = new StackPanel();
        var settingsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto")
        };

        // 打印份数
        AddLabel(settingsGrid, "打印份数:", 0, 0);
        var copiesPanel = new StackPanel
        {
            [Grid.RowProperty] = 0,
            [Grid.ColumnProperty] = 1,
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10)
        };
        _copiesInput = new NumericUpDown 
        { 
            Value = 1, 
            Minimum = 1, 
            Maximum = 99, 
            Width = 150,  // 增加宽度
            Height = 42,
            FontSize = 14
        };
        copiesPanel.Children.Add(_copiesInput);
        copiesPanel.Children.Add(new TextBlock { Text = "份", Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 14 });
        settingsGrid.Children.Add(copiesPanel);

        // 页码范围
        AddLabel(settingsGrid, "页码范围:", 1, 0);
        var pageRangePanel = new StackPanel
        {
            [Grid.RowProperty] = 1,
            [Grid.ColumnProperty] = 1,
            Margin = new Thickness(10)
        };
        _pageRangeCombo = new ComboBox { Height = 42 };
        _pageRangeCombo.Items.Add("全部");
        _pageRangeCombo.Items.Add("自定义");
        _pageRangeCombo.SelectedIndex = 0;
        _pageRangeCombo.SelectionChanged += (s, e) =>
        {
            if (_customPageRangeInput != null)
                _customPageRangeInput.IsEnabled = _pageRangeCombo.SelectedIndex == 1;
        };
        pageRangePanel.Children.Add(_pageRangeCombo);
        _customPageRangeInput = new TextBox
        {
            Watermark = "例如: 1-5,8,11-13",
            Margin = new Thickness(0, 8, 0, 0),
            Height = 42,
            IsEnabled = false
        };
        pageRangePanel.Children.Add(_customPageRangeInput);
        settingsGrid.Children.Add(pageRangePanel);

        // 双面打印
        AddLabel(settingsGrid, "双面打印:", 2, 0);
        _duplexCombo = new ComboBox
        {
            [Grid.RowProperty] = 2,
            [Grid.ColumnProperty] = 1,
            Margin = new Thickness(10),
            Height = 42
        };
        _duplexCombo.Items.Add("单面");
        _duplexCombo.Items.Add("双面 (长边翻转)");
        _duplexCombo.Items.Add("双面 (短边翻转)");
        _duplexCombo.SelectedIndex = 0;
        settingsGrid.Children.Add(_duplexCombo);

        // 颜色模式
        AddLabel(settingsGrid, "颜色模式:", 3, 0);
        _colorCombo = new ComboBox
        {
            [Grid.RowProperty] = 3,
            [Grid.ColumnProperty] = 1,
            Margin = new Thickness(10),
            Height = 42
        };
        _colorCombo.Items.Add("黑白");
        _colorCombo.Items.Add("彩色");
        _colorCombo.SelectedIndex = 1;
        settingsGrid.Children.Add(_colorCombo);

        // 纸张大小
        AddLabel(settingsGrid, "纸张大小:", 4, 0);
        _paperSizeCombo = new ComboBox
        {
            [Grid.RowProperty] = 4,
            [Grid.ColumnProperty] = 1,
            Margin = new Thickness(10),
            Height = 42
        };
        _paperSizeCombo.Items.Add("A4 (210 × 297 mm)");
        _paperSizeCombo.Items.Add("A3 (297 × 420 mm)");
        _paperSizeCombo.Items.Add("A5 (148 × 210 mm)");
        _paperSizeCombo.Items.Add("Letter (8.5 × 11 in)");
        _paperSizeCombo.Items.Add("Legal (8.5 × 14 in)");
        _paperSizeCombo.SelectedIndex = 0;
        settingsGrid.Children.Add(_paperSizeCombo);

        // 打印方向
        AddLabel(settingsGrid, "打印方向:", 5, 0);
        _orientationCombo = new ComboBox
        {
            [Grid.RowProperty] = 5,
            [Grid.ColumnProperty] = 1,
            Margin = new Thickness(10),
            Height = 42
        };
        _orientationCombo.Items.Add("纵向 (Portrait)");
        _orientationCombo.Items.Add("横向 (Landscape)");
        _orientationCombo.SelectedIndex = 0;
        settingsGrid.Children.Add(_orientationCombo);

        settingsPanel.Children.Add(settingsGrid);

        // 高级选项
        var advancedPanel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        _fitToPageCheck = new CheckBox { Content = "适应页面大小", IsChecked = true, Margin = new Thickness(0, 8, 0, 0) };
        advancedPanel.Children.Add(_fitToPageCheck);
        _autoRotateCheck = new CheckBox { Content = "自动旋转和居中", IsChecked = true, Margin = new Thickness(0, 8, 0, 0) };
        advancedPanel.Children.Add(_autoRotateCheck);

        var scaleGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        scaleGrid.Children.Add(new TextBlock { Text = "缩放比例:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) });
        var scalePanel = new StackPanel
        {
            [Grid.ColumnProperty] = 1,
            Orientation = Orientation.Horizontal
        };
        _scaleSlider = new Slider { Value = 100, Minimum = 50, Maximum = 200, Width = 220, TickFrequency = 10, IsSnapToTickEnabled = true };
        _scaleSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value" && _scaleText != null)
                _scaleText.Text = $"{(int)_scaleSlider.Value}%";
        };
        scalePanel.Children.Add(_scaleSlider);
        _scaleText = new TextBlock
        {
            Text = "100%",
            Margin = new Thickness(16, 0, 0, 0),
            FontWeight = FontWeight.Bold,
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.Parse("#1976D2"))
        };
        scalePanel.Children.Add(_scaleText);
        scaleGrid.Children.Add(scalePanel);
        advancedPanel.Children.Add(scaleGrid);

        settingsPanel.Children.Add(advancedPanel);

        // 打印按钮
        var printBtn = new Button
        {
            Content = "🖨️ 开始打印",
            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
            Foreground = Brushes.White,
            Margin = new Thickness(0, 24, 0, 0),
            Height = 56,
            FontSize = 17,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        printBtn.Click += OnPrint;
        settingsPanel.Children.Add(printBtn);

        rightPanel.Children.Add(CreateCard("⚙️ 打印设置", settingsPanel));

        // 打印任务
        var jobsPanel = new StackPanel();
        var jobsHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 0, 0, 12)
        };
        jobsHeader.Children.Add(new TextBlock { Text = "打印任务", FontSize = 18, FontWeight = FontWeight.Bold });
        var refreshJobsBtn = new Button
        {
            [Grid.ColumnProperty] = 1,
            Content = "🔄 刷新",
            Background = new SolidColorBrush(Color.Parse("#607D8B")),
            Foreground = Brushes.White,
            Padding = new Thickness(16, 8)
        };
        refreshJobsBtn.Click += OnRefreshJobs;
        jobsHeader.Children.Add(refreshJobsBtn);
        jobsPanel.Children.Add(jobsHeader);

        _jobsGrid = new DataGrid
        {
            IsReadOnly = true,
            MaxHeight = 400,
            AutoGenerateColumns = false,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        
        // 文件名
        _jobsGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "文件名", 
            Binding = new Binding("FileName"), 
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 120
        });
        
        // 打印机
        _jobsGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "打印机", 
            Binding = new Binding("PrinterName"), 
            Width = new DataGridLength(110) 
        });
        
        // 份数
        _jobsGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "份数", 
            Binding = new Binding("Copies"), 
            Width = new DataGridLength(50) 
        });
        
        // 页数
        _jobsGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "页数", 
            Binding = new Binding("PagesDisplay"), 
            Width = new DataGridLength(70) 
        });
        
        // 费用
        _jobsGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "费用", 
            Binding = new Binding("CostDisplay"), 
            Width = new DataGridLength(70) 
        });
        
        // 状态
        _jobsGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "状态", 
            Binding = new Binding("StatusDisplay"), 
            Width = new DataGridLength(110) 
        });
        
        // 时间
        _jobsGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "时间", 
            Binding = new Binding("CreatedAtDisplay"), 
            Width = new DataGridLength(80) 
        });
        
        jobsPanel.Children.Add(_jobsGrid);

        rightPanel.Children.Add(CreateCard("📋 打印任务", jobsPanel));

        contentGrid.Children.Add(rightPanel);
        
        // 将 contentGrid 放入 ScrollViewer
        scrollViewer.Content = contentGrid;
        mainGrid.Children.Add(scrollViewer);

        return mainGrid;
    }

    private Border CreateCard(string title, Control content)
    {
        return new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24),
            Margin = new Thickness(0, 0, 0, 16),
            BoxShadow = new BoxShadows(new BoxShadow { Blur = 15, OffsetY = 3, Color = Color.Parse("#CCCCCC") }),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 16) },
                    content
                }
            }
        };
    }

    private void AddLabel(Grid grid, string text, int row, int column)
    {
        var label = new TextBlock
        {
            [Grid.RowProperty] = row,
            [Grid.ColumnProperty] = column,
            Text = text,
            FontSize = 14,
            FontWeight = FontWeight.Medium,
            Foreground = new SolidColorBrush(Color.Parse("#666666")),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 10)
        };
        grid.Children.Add(label);
    }

    private void BindData()
    {
        if (_viewModel == null) return;

        // 监听 Printers 集合变化
        if (_printerCombo != null)
        {
            _viewModel.Printers.CollectionChanged += (s, e) =>
            {
                _printerCombo.Items.Clear();
                foreach (var printer in _viewModel.Printers)
                {
                    _printerCombo.Items.Add(printer);
                }
                if (_printerCombo.Items.Count > 0 && _printerCombo.SelectedIndex < 0)
                    _printerCombo.SelectedIndex = 0;
            };
        }

        // 监听其他属性变化
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.StatusMessage) && _statusText != null)
            {
                _statusText.Text = _viewModel.StatusMessage;
            }
            else if (e.PropertyName == nameof(MainViewModel.SelectedFileName) && _fileNameText != null)
            {
                _fileNameText.Text = _viewModel.SelectedFileName;
            }
        };
        
        // 监听 Jobs 集合变化
        if (_jobsGrid != null)
        {
            _viewModel.Jobs.CollectionChanged += (s, e) =>
            {
                _jobsGrid.ItemsSource = null;
                _jobsGrid.ItemsSource = _viewModel.Jobs;
            };
            
            // 初始化任务列表
            _jobsGrid.ItemsSource = _viewModel.Jobs;
        }
    }

    private void OnRefreshPrinters(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.GetPrintersCommand.Execute().Subscribe();
        }
    }

    private async void OnSelectFile(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要打印的文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("所有支持的文件") { Patterns = new[] { "*.pdf", "*.png", "*.jpg", "*.jpeg", "*.docx", "*.xlsx", "*.txt" } },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0 && _viewModel != null)
        {
            _viewModel.OnFileSelected(files[0].Path.LocalPath);
        }
    }

    private void OnPrint(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // 更新 ViewModel 的值
        if (_printerCombo?.SelectedItem is string printer)
            _viewModel.SelectedPrinter = printer;
        
        if (_copiesInput != null)
            _viewModel.Copies = (int)_copiesInput.Value.GetValueOrDefault();
        
        if (_pageRangeCombo != null)
            _viewModel.PageRangeMode = _pageRangeCombo.SelectedIndex;
        
        if (_customPageRangeInput != null)
            _viewModel.CustomPageRange = _customPageRangeInput.Text ?? "";
        
        if (_duplexCombo != null)
            _viewModel.DuplexMode = _duplexCombo.SelectedIndex;
        
        if (_colorCombo != null)
            _viewModel.ColorMode = _colorCombo.SelectedIndex;
        
        if (_paperSizeCombo != null)
            _viewModel.PaperSizeMode = _paperSizeCombo.SelectedIndex;
        
        if (_orientationCombo != null)
            _viewModel.OrientationMode = _orientationCombo.SelectedIndex;
        
        if (_scaleSlider != null)
            _viewModel.ScalePercent = (int)_scaleSlider.Value;
        
        if (_fitToPageCheck != null)
            _viewModel.FitToPage = _fitToPageCheck.IsChecked ?? true;
        
        if (_autoRotateCheck != null)
            _viewModel.AutoRotateAndCenter = _autoRotateCheck.IsChecked ?? true;

        _viewModel.UploadAndPrintCommand.Execute().Subscribe();
    }

    private void OnRefreshJobs(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.RefreshJobsCommand.Execute().Subscribe();
        }
    }
}
