namespace PrintServer.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly string[] _protectedPaths = { "/api/upload", "/api/print" };

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // 只检查 POST 请求的受保护路径
        if (context.Request.Method == "POST" && _protectedPaths.Any(p => path.StartsWith(p)))
        {
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "缺少 API Key" });
                return;
            }

            var configuredKey = _configuration["ApiKey"] ?? "dev-token-123";
            if (apiKey != configuredKey)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "无效的 API Key" });
                return;
            }
        }

        await _next(context);
    }
}
