using PrintServer.Services;
using PrintServer.Middleware;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// 配置文件上传大小限制
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});

// 配置服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册自定义服务
builder.Services.AddSingleton<IPrinterService, PrinterService>();
builder.Services.AddSingleton<IUploadService, UploadService>();
builder.Services.AddSingleton<IJobStore, JobStore>();
builder.Services.AddSingleton<IPrintQueueService, PrintQueueService>();
builder.Services.AddSingleton<IPrintStatisticsService, PrintStatisticsService>();
builder.Services.AddSingleton<IFilePreviewService, FilePreviewService>();

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

// 启动打印队列服务
var printQueue = app.Services.GetRequiredService<IPrintQueueService>();
_ = printQueue.StartProcessingAsync();

Console.WriteLine("打印服务端已启动");
Console.WriteLine($"监听地址: {builder.Configuration["Urls"] ?? "http://10.22.19.132:5000"}");
Console.WriteLine($"API Key: {builder.Configuration["ApiKey"] ?? "dev-token-123"}");

app.Run();
