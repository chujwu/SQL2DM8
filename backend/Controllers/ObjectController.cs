using Microsoft.AspNetCore.Mvc;
using SQLServerToDM8.Models;
using SQLServerToDM8.Services;

namespace SQLServerToDM8.Controllers;

[ApiController]
[Route("api/objects")]
public class ObjectController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ObjectController> _logger;

    public ObjectController(IDatabaseService databaseService, ILogger<ObjectController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// 测试对象树连接
    /// </summary>
    [HttpPost("{database}/test")]
    public async Task<ActionResult<object>> TestObjectConnection(
        string database,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        try
        {
            _logger.LogInformation("测试对象连接: 数据库={Database}, 服务器={Server}", 
                database, connectionInfo.Server);
            
            var objects = await _databaseService.GetObjectsAsync(connectionInfo, database);
            
            return Ok(new {
                success = true,
                message = "连接成功",
                objectCount = objects.Count,
                views = objects.Count(o => o.Type == DatabaseObjectType.View),
                functions = objects.Count(o => o.Type == DatabaseObjectType.Function),
                procedures = objects.Count(o => o.Type == DatabaseObjectType.Procedure)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试对象连接失败");
            return BadRequest(new { 
                success = false,
                message = ex.Message,
                details = ex.InnerException?.Message 
            });
        }
    }

    /// <summary>
    /// 调试：获取所有对象类型统计
    /// </summary>
    [HttpPost("{database}/debug")]
    public async Task<ActionResult<object>> DebugObjectTypes(
        string database,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        try
        {
            var connStr = BuildConnectionString(connectionInfo, database);
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connStr);
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    o.type,
                    o.type_desc,
                    COUNT(*) as cnt
                FROM sys.objects o
                WHERE o.is_ms_shipped = 0
                GROUP BY o.type, o.type_desc
                ORDER BY cnt DESC";

            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            var types = new List<object>();
            while (await reader.ReadAsync())
            {
                types.Add(new {
                    type = reader.GetString(0),
                    typeDesc = reader.GetString(1),
                    count = reader.GetInt32(2)
                });
            }

            return Ok(new {
                database,
                objectTypes = types
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 调试：查看前10个对象的类型映射
    /// </summary>
    [HttpPost("{database}/debug-objects")]
    public async Task<ActionResult<object>> DebugObjectMapping(
        string database,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        try
        {
            var connStr = BuildConnectionString(connectionInfo, database);
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connStr);
            await connection.OpenAsync();

            var sql = @"
                SELECT TOP 20
                    o.name AS ObjectName,
                    s.name AS SchemaName,
                    o.type AS TypeCode,
                    o.type_desc AS TypeDesc
                FROM sys.objects o
                INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                WHERE o.is_ms_shipped = 0
                ORDER BY o.type, o.name";

            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            var objects = new List<object>();
            while (await reader.ReadAsync())
            {
                var typeCode = reader.GetString(2);
                var mappedType = typeCode switch
                {
                    "V" => "View",
                    "FN" or "IF" or "TF" or "FS" or "FT" => "Function",
                    "P" or "PC" => "Procedure",
                    _ => "View (default)"
                };

                objects.Add(new {
                    name = reader.GetString(0),
                    schema = reader.GetString(1),
                    typeCode = typeCode,
                    typeDesc = reader.GetString(3),
                    mappedTo = mappedType
                });
            }

            return Ok(new {
                database,
                objects = objects
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string BuildConnectionString(SqlServerConnectionInfo info, string? database = null)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = info.Port == 1433 ? info.Server : $"{info.Server},{info.Port}",
            ConnectTimeout = 15,
            TrustServerCertificate = true
        };

        if (info.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = info.Username;
            builder.Password = info.Password;
        }

        if (!string.IsNullOrEmpty(database))
        {
            builder.InitialCatalog = database;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// 获取对象树
    /// </summary>
    [HttpPost("{database}/tree")]
    public async Task<ActionResult<List<ObjectTreeNode>>> GetObjectTree(
        string database,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        try
        {
            _logger.LogInformation("获取对象树请求: 数据库={Database}, 服务器={Server}", 
                database, connectionInfo.Server);
            
            var objects = await _databaseService.GetObjectsAsync(connectionInfo, database);
            
            _logger.LogInformation("获取到 {Count} 个对象", objects.Count);
            
            var tree = BuildTree(objects);
            return Ok(tree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对象树失败: {Message}", ex.Message);
            return BadRequest(new { 
                message = ex.Message,
                details = ex.InnerException?.Message 
            });
        }
    }

    /// <summary>
    /// 获取视图列表
    /// </summary>
    [HttpPost("{database}/views")]
    public async Task<ActionResult<List<DatabaseObject>>> GetViews(
        string database,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        return await GetObjectsByType(connectionInfo, database, DatabaseObjectType.View);
    }

    /// <summary>
    /// 获取函数列表
    /// </summary>
    [HttpPost("{database}/functions")]
    public async Task<ActionResult<List<DatabaseObject>>> GetFunctions(
        string database,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        return await GetObjectsByType(connectionInfo, database, DatabaseObjectType.Function);
    }

    /// <summary>
    /// 获取存储过程列表
    /// </summary>
    [HttpPost("{database}/procedures")]
    public async Task<ActionResult<List<DatabaseObject>>> GetProcedures(
        string database,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        return await GetObjectsByType(connectionInfo, database, DatabaseObjectType.Procedure);
    }

    /// <summary>
    /// 获取对象的 SQL 定义
    /// </summary>
    [HttpPost("{database}/{type}/{schema}/{name}/sql")]
    public async Task<ActionResult<SqlDefinition>> GetObjectSql(
        string database,
        DatabaseObjectType type,
        string schema,
        string name,
        [FromBody] SqlServerConnectionInfo connectionInfo)
    {
        try
        {
            var definition = await _databaseService.GetObjectSqlAsync(
                connectionInfo, database, schema, name, type);
            return Ok(definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对象 SQL 失败");
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<ActionResult<List<DatabaseObject>>> GetObjectsByType(
        SqlServerConnectionInfo connectionInfo,
        string database,
        DatabaseObjectType type)
    {
        try
        {
            var objects = await _databaseService.GetObjectsAsync(connectionInfo, database, type);
            return Ok(objects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对象列表失败");
            return BadRequest(new { message = ex.Message });
        }
    }

    private List<ObjectTreeNode> BuildTree(List<DatabaseObject> objects)
    {
        var tree = new List<ObjectTreeNode>();

        _logger.LogInformation("构建对象树，对象数量: {Count}", objects.Count);
        
        // 按类型分组
        var grouped = objects.GroupBy(o => o.Type);

        foreach (var group in grouped)
        {
            _logger.LogInformation("类型: {Type}, 数量: {Count}", group.Key, group.Count());
            
            var typeNode = new ObjectTreeNode
            {
                Key = group.Key.ToString(),
                Title = group.Key switch
                {
                    DatabaseObjectType.View => "视图",
                    DatabaseObjectType.Function => "函数",
                    DatabaseObjectType.Procedure => "存储过程",
                    _ => group.Key.ToString()
                },
                IsLeaf = false,
                Icon = group.Key switch
                {
                    DatabaseObjectType.View => "eye",
                    DatabaseObjectType.Function => "function",
                    DatabaseObjectType.Procedure => "database",
                    _ => "file"
                }
            };

            // 按 schema 分组
            var schemaGroups = group.GroupBy(o => o.Schema);
            foreach (var schemaGroup in schemaGroups)
            {
                var schemaNode = new ObjectTreeNode
                {
                    Key = $"{group.Key}/{schemaGroup.Key}",
                    Title = schemaGroup.Key,
                    IsLeaf = false,
                    Icon = "folder"
                };

                // 添加对象
                foreach (var obj in schemaGroup)
                {
                    schemaNode.Children.Add(new ObjectTreeNode
                    {
                        Key = $"{group.Key}/{schemaGroup.Key}/{obj.Name}",
                        Title = obj.Name,
                        IsLeaf = true,
                        Icon = "file"
                    });
                }

                typeNode.Children.Add(schemaNode);
            }

            tree.Add(typeNode);
        }

        _logger.LogInformation("对象树构建完成，类型数量: {Count}", tree.Count);
        return tree;
    }
}
