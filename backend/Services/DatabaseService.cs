using System.Diagnostics;
using Microsoft.Data.SqlClient;
using SQLServerToDM8.Models;

namespace SQLServerToDM8.Services;

public interface IDatabaseService
{
    Task<ConnectionTestResult> TestConnectionAsync(SqlServerConnectionInfo connectionInfo);
    Task<List<DatabaseInfo>> GetDatabasesAsync(SqlServerConnectionInfo connectionInfo);
    Task<List<DatabaseObject>> GetObjectsAsync(SqlServerConnectionInfo connectionInfo, string database, DatabaseObjectType? type = null);
    Task<SqlDefinition> GetObjectSqlAsync(SqlServerConnectionInfo connectionInfo, string database, string schema, string name, DatabaseObjectType type);
}

public class DatabaseService : IDatabaseService
{
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
    }

    private string BuildConnectionString(SqlServerConnectionInfo info, string? database = null)
    {
        var builder = new SqlConnectionStringBuilder
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

    public async Task<ConnectionTestResult> TestConnectionAsync(SqlServerConnectionInfo connectionInfo)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var connStr = BuildConnectionString(connectionInfo, connectionInfo.Database);
            using var connection = new SqlConnection(connStr);
            await connection.OpenAsync();
            sw.Stop();

            return new ConnectionTestResult
            {
                Success = true,
                Message = "连接成功",
                ElapsedMs = sw.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "连接测试失败");
            return new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message,
                ElapsedMs = sw.Elapsed.TotalMilliseconds
            };
        }
    }

    public async Task<List<DatabaseInfo>> GetDatabasesAsync(SqlServerConnectionInfo connectionInfo)
    {
        var connStr = BuildConnectionString(connectionInfo);
        using var connection = new SqlConnection(connStr);
        await connection.OpenAsync();

        var databases = new List<DatabaseInfo>();
        using var cmd = new SqlCommand(@"
            SELECT database_id, name, state_desc
            FROM sys.databases
            WHERE database_id > 4  -- 排除系统数据库
            ORDER BY name", connection);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            databases.Add(new DatabaseInfo
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                State = reader.GetString(2)
            });
        }

        return databases;
    }

    public async Task<List<DatabaseObject>> GetObjectsAsync(
        SqlServerConnectionInfo connectionInfo,
        string database,
        DatabaseObjectType? type = null)
    {
        _logger.LogInformation("获取数据库对象: 服务器={Server}, 数据库={Database}", 
            connectionInfo.Server, database);
        
        var connStr = BuildConnectionString(connectionInfo, database);
        _logger.LogDebug("连接字符串: {ConnectionString}", connStr.Replace(connectionInfo.Password ?? "", "***"));
        
        using var connection = new SqlConnection(connStr);
        await connection.OpenAsync();

        var objects = new List<DatabaseObject>();

        var sql = @"
            SELECT
                o.name AS ObjName,
                s.name AS SchemaName,
                o.type AS TypeCode,
                o.type_desc AS TypeDesc,
                o.modify_date AS ModifyDate
            FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.type IN ('V', 'P', 'PC', 'FN', 'IF', 'TF', 'FS', 'FT')
                AND o.is_ms_shipped = 0";

        if (type.HasValue)
        {
            sql += type.Value switch
            {
                DatabaseObjectType.View => " AND o.type = 'V'",
                DatabaseObjectType.Function => " AND o.type IN ('FN', 'IF', 'TF', 'FS', 'FT')",
                DatabaseObjectType.Procedure => " AND o.type IN ('P', 'PC')",
                _ => ""
            };
        }

        sql += " ORDER BY o.type, s.name, o.name";

        _logger.LogDebug("执行 SQL: {Sql}", sql);

        using var cmd = new SqlCommand(sql, connection);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var name = reader["ObjName"]?.ToString()?.Trim() ?? string.Empty;
            var schema = reader["SchemaName"]?.ToString()?.Trim() ?? "dbo";
            var typeCode = reader["TypeCode"]?.ToString()?.Trim() ?? string.Empty;
            var typeDesc = reader["TypeDesc"]?.ToString()?.Trim() ?? string.Empty;
            var modifyDate = reader.GetDateTime(reader.GetOrdinal("ModifyDate"));
            
            DatabaseObjectType objectType;
            switch (typeCode)
            {
                case "V":
                    objectType = DatabaseObjectType.View;
                    break;
                case "FN":
                case "IF":
                case "TF":
                case "FS":
                case "FT":
                    objectType = DatabaseObjectType.Function;
                    break;
                case "P":
                case "PC":
                    objectType = DatabaseObjectType.Procedure;
                    break;
                default:
                    objectType = DatabaseObjectType.View;
                    break;
            }

            _logger.LogInformation("对象: {Name}, Schema: {Schema}, 类型代码: [{TypeCode}], 类型描述: {TypeDesc}, 映射为: {ObjectType}", 
                name, schema, typeCode, typeDesc, objectType);

            objects.Add(new DatabaseObject
            {
                Name = name,
                Schema = schema,
                Type = objectType,
                ModifyDate = modifyDate
            });
        }

        _logger.LogInformation("获取到 {Count} 个对象", objects.Count);
        return objects;
    }

    public async Task<SqlDefinition> GetObjectSqlAsync(
        SqlServerConnectionInfo connectionInfo,
        string database,
        string schema,
        string name,
        DatabaseObjectType type)
    {
        var connStr = BuildConnectionString(connectionInfo, database);
        using var connection = new SqlConnection(connStr);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                o.name,
                s.name,
                o.type_desc,
                m.definition
            FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            INNER JOIN sys.sql_modules m ON o.object_id = m.object_id
            WHERE o.name = @Name
                AND s.name = @Schema";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Name", name);
        cmd.Parameters.AddWithValue("@Schema", schema);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SqlDefinition
            {
                ObjectName = reader.GetString(0),
                Schema = reader.GetString(1),
                ObjectType = type,
                Sql = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            };
        }

        throw new InvalidOperationException($"未找到对象: {schema}.{name}");
    }
}
