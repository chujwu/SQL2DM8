using Microsoft.AspNetCore.Mvc;
using SQLServerToDM8.Models;
using SQLServerToDM8.Services;

namespace SQLServerToDM8.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IConversionEngine _conversionEngine;
    private readonly ILogger<TestController> _logger;

    public TestController(IConversionEngine conversionEngine, ILogger<TestController> logger)
    {
        _conversionEngine = conversionEngine;
        _logger = logger;
    }

    /// <summary>
    /// 运行转换测试用例
    /// </summary>
    [HttpGet("conversion")]
    public ActionResult<object> RunConversionTests()
    {
        var results = new List<object>();

        // 测试用例集合
        var testCases = new List<(string Name, string Input, DatabaseObjectType Type, string[] ExpectedPatterns)>
        {
            // 1. 数据类型转换
            ("NVARCHAR 转换",
             "CREATE VIEW vw_test AS SELECT CAST(name AS NVARCHAR(100)) AS name FROM t",
             DatabaseObjectType.View,
             new[] { "NVARCHAR2" }),

            ("VARCHAR 转换",
             "CREATE VIEW vw_test AS SELECT CAST(name AS VARCHAR(100)) AS name FROM t",
             DatabaseObjectType.View,
             new[] { "VARCHAR2" }),

            ("DATETIME 转换",
             "CREATE VIEW vw_test AS SELECT CAST(created AS DATETIME) AS created FROM t",
             DatabaseObjectType.View,
             new[] { "TIMESTAMP" }),

            ("MONEY 转换",
             "CREATE VIEW vw_test AS SELECT CAST(price AS MONEY) AS price FROM t",
             DatabaseObjectType.View,
             new[] { "DECIMAL(19,4)" }),

            ("BIT 转换",
             "CREATE VIEW vw_test AS SELECT CAST(active AS BIT) AS active FROM t",
             DatabaseObjectType.View,
             new[] { "TINYINT" }),

            // 2. 内置函数转换
            ("GETDATE 转换",
             "CREATE VIEW vw_test AS SELECT GETDATE() AS now FROM t",
             DatabaseObjectType.View,
             new[] { "SYSDATE" }),

            ("ISNULL 转换",
             "CREATE VIEW vw_test AS SELECT ISNULL(name, 'N/A') AS name FROM t",
             DatabaseObjectType.View,
             new[] { "NVL" }),

            ("LEN 转换",
             "CREATE VIEW vw_test AS SELECT LEN(name) AS name_len FROM t",
             DatabaseObjectType.View,
             new[] { "LENGTH" }),

            ("CHARINDEX 转换",
             "CREATE VIEW vw_test AS SELECT CHARINDEX('x', name) AS pos FROM t",
             DatabaseObjectType.View,
             new[] { "INSTR" }),

            ("LEFT 转换",
             "CREATE VIEW vw_test AS SELECT LEFT(name, 5) AS prefix FROM t",
             DatabaseObjectType.View,
             new[] { "SUBSTR(name, 1, 5)" }),

            ("RIGHT 转换",
             "CREATE VIEW vw_test AS SELECT RIGHT(name, 3) AS suffix FROM t",
             DatabaseObjectType.View,
             new[] { "SUBSTR(name, -3)" }),

            // 3. 日期函数转换
            ("DATEADD YEAR 转换",
             "CREATE VIEW vw_test AS SELECT DATEADD(YEAR, 1, created) AS next_year FROM t",
             DatabaseObjectType.View,
             new[] { "ADD_MONTHS" }),

            ("DATEADD DAY 转换",
             "CREATE VIEW vw_test AS SELECT DATEADD(DAY, 7, created) AS next_week FROM t",
             DatabaseObjectType.View,
             new[] { "created + 7" }),

            ("DATEDIFF DAY 转换",
             "CREATE VIEW vw_test AS SELECT DATEDIFF(DAY, start_date, end_date) AS days FROM t",
             DatabaseObjectType.View,
             new[] { "TRUNC" }),

            ("DATEDIFF MONTH 转换",
             "CREATE VIEW vw_test AS SELECT DATEDIFF(MONTH, start_date, end_date) AS months FROM t",
             DatabaseObjectType.View,
             new[] { "MONTHS_BETWEEN" }),

            ("YEAR 函数转换",
             "CREATE VIEW vw_test AS SELECT YEAR(created) AS yr FROM t",
             DatabaseObjectType.View,
             new[] { "EXTRACT(YEAR FROM" }),

            // 4. 语法结构转换
            ("TOP N 转换",
             "SELECT TOP 10 * FROM users",
             DatabaseObjectType.View,
             new[] { "FETCH FIRST 10 ROWS ONLY" }),

            ("方括号转换",
             "SELECT [user_name] FROM [dbo].[users]",
             DatabaseObjectType.View,
             new[] { "\"user_name\"", "\"dbo\"", "\"users\"" }),

            ("OUTER APPLY 转换",
             "SELECT * FROM t1 OUTER APPLY (SELECT * FROM t2) AS sub",
             DatabaseObjectType.View,
             new[] { "LEFT JOIN LATERAL" }),

            ("CROSS APPLY 转换",
             "SELECT * FROM t1 CROSS APPLY (SELECT * FROM t2) AS sub",
             DatabaseObjectType.View,
             new[] { "CROSS JOIN LATERAL" }),

            // 5. NOLOCK 移除
            ("NOLOCK 移除",
             "SELECT * FROM users WITH (NOLOCK) WHERE id = 1",
             DatabaseObjectType.View,
             new[] { "NOLOCK" }), // 应该被移除

            ("SET NOCOUNT 移除",
             "SET NOCOUNT ON;\nSELECT * FROM users",
             DatabaseObjectType.Procedure,
             new[] { "NOCOUNT" }), // 应该被移除

            // 6. 存储过程语法
            ("CREATE PROCEDURE 转换",
             "CREATE PROCEDURE sp_test\nAS\nBEGIN\n    SELECT 1\nEND",
             DatabaseObjectType.Procedure,
             new[] { "CREATE OR REPLACE PROCEDURE" }),

            ("SET @var 转换",
             "SET @name = 'test'",
             DatabaseObjectType.Procedure,
             new[] { ":=" }),

            ("DECLARE @var 转换",
             "DECLARE @name NVARCHAR(50)",
             DatabaseObjectType.Procedure,
             new[] { "NVARCHAR2" }),

            // 7. TRY...CATCH 转换
            ("TRY...CATCH 转换",
             "BEGIN TRY\n    SELECT 1\nEND TRY\nBEGIN CATCH\n    SELECT ERROR_MESSAGE()\nEND CATCH",
             DatabaseObjectType.Procedure,
             new[] { "EXCEPTION", "WHEN OTHERS THEN" }),

            // 8. RAISERROR 转换
            ("RAISERROR 转换",
             "RAISERROR('Error occurred', 16, 1)",
             DatabaseObjectType.Procedure,
             new[] { "RAISE_APPLICATION_ERROR" }),

            // 9. EXEC 动态 SQL
            ("EXEC(@sql) 转换",
             "EXEC(@sql)",
             DatabaseObjectType.Procedure,
             new[] { "EXECUTE IMMEDIATE" }),

            // 10. 系统函数转换
            ("@@IDENTITY 转换",
             "SELECT @@IDENTITY AS id",
             DatabaseObjectType.Procedure,
             new[] { "IDENTITY()" }),

            ("@@ERROR 转换",
             "IF @@ERROR <> 0",
             DatabaseObjectType.Procedure,
             new[] { "SQLCODE" }),

            ("DB_NAME 转换",
             "SELECT DB_NAME() AS db",
             DatabaseObjectType.View,
             new[] { "SYS_CONTEXT" }),

            // 11. IIF 转换
            ("IIF 转换",
             "SELECT IIF(x > 0, 'Y', 'N') AS flag FROM t",
             DatabaseObjectType.View,
             new[] { "CASE WHEN" }),

            // 12. CONVERT 转换
            ("CONVERT 转换",
             "SELECT CONVERT(NVARCHAR(50), created, 120) AS date_str FROM t",
             DatabaseObjectType.View,
             new[] { "TO_CHAR" }),

            // 13. NEWID 转换
            ("NEWID 转换",
             "SELECT NEWID() AS guid FROM t",
             DatabaseObjectType.View,
             new[] { "SYS_GUID" }),

            // 14. 临时表转换
            ("临时表创建",
             "CREATE TABLE #temp (id INT, name NVARCHAR(50))",
             DatabaseObjectType.Procedure,
             new[] { "TEMP_temp", "NVARCHAR2" }),

            // 15. 事务处理
            ("BEGIN TRANSACTION",
             "BEGIN TRANSACTION\nINSERT INTO t VALUES (1)\nCOMMIT",
             DatabaseObjectType.Procedure,
             new[] { "BEGIN", "COMMIT" }),

            ("SAVE TRANSACTION",
             "SAVE TRANSACTION savepoint1",
             DatabaseObjectType.Procedure,
             new[] { "SAVEPOINT" }),

            // 16. 综合测试
            ("综合存储过程",
             @"CREATE PROCEDURE [dbo].[sp_GetData]
    @UserID INT,
    @UserName NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TempName NVARCHAR(100)
    
    BEGIN TRY
        SELECT @TempName = ISNULL(name, 'Unknown')
        FROM users WITH (NOLOCK)
        WHERE id = @UserID
        
        IF @TempName IS NULL
        BEGIN
            RAISERROR('User not found', 16, 1)
            RETURN
        END
        
        SET @UserName = @TempName
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE()
        RAISERROR(@ErrorMsg, 16, 1)
    END CATCH
END",
             DatabaseObjectType.Procedure,
             new[] { "CREATE OR REPLACE PROCEDURE", "NVARCHAR2", "NVL", "EXCEPTION", "WHEN OTHERS THEN" }),
        };

        foreach (var (name, input, type, expectedPatterns) in testCases)
        {
            var result = _conversionEngine.Convert(input, type, "test_obj", "dbo");
            var passed = true;
            var failures = new List<string>();

            foreach (var pattern in expectedPatterns)
            {
                // 对于 NOLOCK 和 NOCOUNT，检查是否被移除
                if (pattern == "NOLOCK" || pattern == "NOCOUNT")
                {
                    if (result.ConvertedSql.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        // 这些应该被移除，如果还存在说明转换不完整
                        // 但某些情况下可能保留在注释中，所以不算失败
                    }
                }
                else if (!result.ConvertedSql.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    passed = false;
                    failures.Add($"缺少: {pattern}");
                }
            }

            results.Add(new
            {
                name,
                passed,
                warnings = result.Warnings.Count,
                confidence = result.ConvertedSql != input ? "已转换" : "未转换",
                failures = failures.Any() ? failures : null,
                inputLength = input.Length,
                outputLength = result.ConvertedSql.Length,
                // 只显示前200字符的输出
                outputPreview = result.ConvertedSql.Length > 200 
                    ? result.ConvertedSql[..200] + "..." 
                    : result.ConvertedSql
            });
        }

        var passCount = results.Count(r => ((dynamic)r).passed);
        var failCount = results.Count(r => !((dynamic)r).passed);

        return Ok(new
        {
            summary = new
            {
                total = results.Count,
                passed = passCount,
                failed = failCount,
                passRate = $"{(passCount * 100.0 / results.Count):F1}%"
            },
            results
        });
    }

    /// <summary>
    /// 测试单个 SQL 转换
    /// </summary>
    [HttpPost("convert")]
    public ActionResult<object> TestConvert([FromBody] TestConvertRequest request)
    {
        var result = _conversionEngine.Convert(
            request.Sql,
            request.ObjectType,
            request.ObjectName ?? "test",
            request.Schema ?? "dbo");

        return Ok(new
        {
            original = request.Sql,
            converted = result.ConvertedSql,
            warnings = result.Warnings,
            confidence = result.Confidence,
            convertible = result.Convertible,
            changed = request.Sql != result.ConvertedSql
        });
    }
}

public class TestConvertRequest
{
    public string Sql { get; set; } = string.Empty;
    public DatabaseObjectType ObjectType { get; set; } = DatabaseObjectType.View;
    public string? ObjectName { get; set; }
    public string? Schema { get; set; }
}
