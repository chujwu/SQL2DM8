using Microsoft.AspNetCore.Mvc;
using SQLServerToDM8.Models;
using SQLServerToDM8.Services;

namespace SQLServerToDM8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConvertController : ControllerBase
{
    private readonly IConversionEngine _conversionEngine;
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly ILogger<ConvertController> _logger;

    public ConvertController(
        IConversionEngine conversionEngine,
        IDatabaseService databaseService,
        IExportService exportService,
        ILogger<ConvertController> logger)
    {
        _conversionEngine = conversionEngine;
        _databaseService = databaseService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// 转换单个 SQL
    /// </summary>
    [HttpPost("single")]
    public async Task<ActionResult<ConvertResult>> ConvertSingle([FromBody] ConvertSingleRequest request)
    {
        try
        {
            var result = _conversionEngine.Convert(
                request.Sql,
                request.ObjectType,
                request.ObjectName,
                request.Schema ?? "dbo");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转换失败");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 批量转换
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<BatchConvertResult>> ConvertBatch(
        [FromBody] BatchConvertRequest request,
        [FromQuery] string server,
        [FromQuery] int port = 1433,
        [FromQuery] bool useWindowsAuth = false,
        [FromQuery] string? username = null,
        [FromQuery] string? password = null)
    {
        try
        {
            var connectionInfo = new SqlServerConnectionInfo
            {
                Server = server,
                Port = port,
                UseWindowsAuth = useWindowsAuth,
                Username = username,
                Password = password,
                Database = request.Database
            };

            var results = new List<ConvertResult>();

            foreach (var obj in request.Objects)
            {
                try
                {
                    // 获取对象 SQL
                    var sqlDef = await _databaseService.GetObjectSqlAsync(
                        connectionInfo, request.Database, obj.Schema, obj.Name, obj.Type);

                    // 转换
                    var result = _conversionEngine.Convert(
                        sqlDef.Sql, obj.Type, obj.Name, obj.Schema);

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "转换对象 {Schema}.{Name} 失败", obj.Schema, obj.Name);
                    results.Add(new ConvertResult
                    {
                        ObjectName = obj.Name,
                        Schema = obj.Schema,
                        ObjectType = obj.Type,
                        OriginalSql = string.Empty,
                        ConvertedSql = string.Empty,
                        Warnings = new List<ConvertWarning>
                        {
                            new() { Message = $"获取或转换失败: {ex.Message}", Severity = WarningSeverity.Error }
                        },
                        Confidence = 0,
                        Convertible = false
                    });
                }
            }

            var batchResult = new BatchConvertResult
            {
                Results = results,
                TotalCount = results.Count,
                SuccessCount = results.Count(r => r.Convertible && !r.Warnings.Any(w => w.Severity == WarningSeverity.Error)),
                WarningCount = results.Count(r => r.Warnings.Any(w => w.Severity == WarningSeverity.Warning)),
                ErrorCount = results.Count(r => !r.Convertible || r.Warnings.Any(w => w.Severity == WarningSeverity.Error))
            };

            return Ok(batchResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量转换失败");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 获取转换规则列表
    /// </summary>
    [HttpGet("rules")]
    public ActionResult<List<ConvertRule>> GetRules()
    {
        var rules = _conversionEngine.GetRules();
        return Ok(rules);
    }

    /// <summary>
    /// 导出转换结果为 ZIP
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportToZip([FromBody] List<ConvertResult> results)
    {
        try
        {
            var zipBytes = await _exportService.ExportToZipAsync(results);
            return File(zipBytes, "application/zip", "dm8_converted_objects.zip");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出失败");
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class ConvertSingleRequest
{
    public string Sql { get; set; } = string.Empty;
    public DatabaseObjectType ObjectType { get; set; }
    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
