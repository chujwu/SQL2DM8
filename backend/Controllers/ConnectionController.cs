using Microsoft.AspNetCore.Mvc;
using SQLServerToDM8.Models;
using SQLServerToDM8.Services;

namespace SQLServerToDM8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ConnectionController> _logger;

    public ConnectionController(IDatabaseService databaseService, ILogger<ConnectionController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<ConnectionTestResult>> TestConnection([FromBody] SqlServerConnectionInfo connectionInfo)
    {
        _logger.LogInformation("测试连接到 {Server}", connectionInfo.Server);
        var result = await _databaseService.TestConnectionAsync(connectionInfo);
        return Ok(result);
    }

    /// <summary>
    /// 获取数据库列表
    /// </summary>
    [HttpPost("databases")]
    public async Task<ActionResult<List<DatabaseInfo>>> GetDatabases([FromBody] SqlServerConnectionInfo connectionInfo)
    {
        try
        {
            var databases = await _databaseService.GetDatabasesAsync(connectionInfo);
            return Ok(databases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据库列表失败");
            return BadRequest(new { message = ex.Message });
        }
    }
}
