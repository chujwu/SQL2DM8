using System.IO.Compression;
using System.Text;
using SQLServerToDM8.Models;

namespace SQLServerToDM8.Services;

public interface IExportService
{
    Task<byte[]> ExportToZipAsync(List<ConvertResult> results);
}

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ExportToZipAsync(List<ConvertResult> results)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var result in results)
            {
                if (string.IsNullOrWhiteSpace(result.ConvertedSql))
                    continue;

                // 按对象类型分目录
                var directory = result.ObjectType switch
                {
                    DatabaseObjectType.View => "Views",
                    DatabaseObjectType.Function => "Functions",
                    DatabaseObjectType.Procedure => "Procedures",
                    _ => "Other"
                };

                // 文件名：{schema}.{objectName}.sql
                var fileName = $"{result.Schema}.{result.ObjectName}.sql";
                var entryPath = $"{directory}/{fileName}";

                var entry = archive.CreateEntry(entryPath);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);

                // 写入文件头注释
                await writer.WriteLineAsync($"-- 对象: {result.Schema}.{result.ObjectName}");
                await writer.WriteLineAsync($"-- 类型: {result.ObjectType}");
                await writer.WriteLineAsync($"-- 转换置信度: {result.Confidence:P0}");

                if (result.Warnings.Any())
                {
                    await writer.WriteLineAsync("-- 警告:");
                    foreach (var warning in result.Warnings)
                    {
                        await writer.WriteLineAsync($"--   [{warning.Severity}] {warning.Message}");
                    }
                }

                await writer.WriteLineAsync("--");
                await writer.WriteLineAsync();

                // 写入转换后的 SQL
                await writer.WriteAsync(result.ConvertedSql);
            }
        }

        return memoryStream.ToArray();
    }
}
