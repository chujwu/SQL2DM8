using SQLServerToDM8.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SQL2DM8 API", Version = "v1" });
});

// 注册自定义服务
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IConversionEngine, ConversionEngine>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddSingleton<ISampleDataService, SampleDataService>();

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 启用 CORS
app.UseCors("AllowFrontend");

// 添加健康检查端点
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

// 启动信息
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=".PadRight(50, '='));
logger.LogInformation("SQL2DM8 - SQL Server to DM8 Converter");
logger.LogInformation("=".PadRight(50, '='));
logger.LogInformation("API 地址: http://localhost:5000");
logger.LogInformation("Swagger UI: http://localhost:5000/swagger");
logger.LogInformation("健康检查: http://localhost:5000/api/health");
logger.LogInformation("=".PadRight(50, '='));

app.Run();
