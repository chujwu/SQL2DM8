using System.Text;
using System.Text.RegularExpressions;
using SQLServerToDM8.Models;

namespace SQLServerToDM8.Services;

public interface IConversionEngine
{
    ConvertResult Convert(string sql, DatabaseObjectType objectType, string objectName, string schema = "dbo");
    List<ConvertRule> GetRules();
}

public class ConvertRule
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SqlServerPattern { get; set; } = string.Empty;
    public string DM8Replacement { get; set; } = string.Empty;
}

public class ConversionEngine : IConversionEngine
{
    private readonly ILogger<ConversionEngine> _logger;
    private readonly List<ConvertRule> _rules;
    private readonly List<ConvertWarning> _warnings = new();
    private int _loopDepth = 0; // 跟踪循环嵌套深度
    private readonly Dictionary<int, string> _stringPlaceholders = new(); // 字符串占位符映射
    private int _placeholderCounter = 0;

    public ConversionEngine(ILogger<ConversionEngine> logger)
    {
        _logger = logger;
        _rules = InitializeRules();
    }

    public ConvertResult Convert(string sql, DatabaseObjectType objectType, string objectName, string schema = "dbo")
    {
        _warnings.Clear();
        _loopDepth = 0;
        _stringPlaceholders.Clear();
        _placeholderCounter = 0;

        try
        {
            var result = sql;

            // 预处理
            result = PreProcess(result);

            // 数据类型转换
            result = ConvertDataTypes(result);

            // 内置函数转换
            result = ConvertBuiltInFunctions(result);

            // 日期函数展开
            result = ConvertDateFunctions(result);

            // 语法结构转换
            result = ConvertSyntax(result, objectType);

            // 后处理
            result = PostProcess(result);

            // 计算置信度
            var confidence = CalculateConfidence(_warnings.Count);

            return new ConvertResult
            {
                ObjectName = objectName,
                Schema = schema,
                ObjectType = objectType,
                OriginalSql = sql,
                ConvertedSql = result,
                Warnings = new List<ConvertWarning>(_warnings),
                Confidence = confidence,
                Convertible = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转换失败: {ObjectName}", objectName);
            return new ConvertResult
            {
                ObjectName = objectName,
                Schema = schema,
                ObjectType = objectType,
                OriginalSql = sql,
                ConvertedSql = sql,
                Warnings = new List<ConvertWarning>
                {
                    new()
                    {
                        Message = $"转换失败: {ex.Message}",
                        Severity = WarningSeverity.Error
                    }
                },
                Confidence = 0,
                Convertible = false
            };
        }
    }

    private string PreProcess(string sql)
    {
        // 移除 NOLOCK 提示
        sql = Regex.Replace(sql, @"\bWITH\s*\(\s*NOLOCK\s*\)", "", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bWITH\s*\(\s*NOLOCK\s*,\s*NOLOCK\s*\)", "", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\(\s*NOLOCK\s*\)", "", RegexOptions.IgnoreCase);

        // 移除其他表提示
        sql = Regex.Replace(sql, @"\bWITH\s*\(\s*(ROWLOCK|PAGLOCK|TABLOCK|TABLOCKX|HOLDLOCK|UPDLOCK|XLOCK|NOEXPAND)\s*\)", "", RegexOptions.IgnoreCase);

        // 移除 SET NOCOUNT ON
        sql = Regex.Replace(sql, @"SET\s+NOCOUNT\s+(ON|OFF)\s*;?\s*\n?", "", RegexOptions.IgnoreCase);

        // 移除 SET TRANSACTION ISOLATION LEVEL
        sql = Regex.Replace(sql, @"SET\s+TRANSACTION\s+ISOLATION\s+LEVEL\s+\w+\s*;?\s*\n?", "", RegexOptions.IgnoreCase);

        // 标准化空白
        sql = Regex.Replace(sql, @"[ \t]+", " ");
        sql = Regex.Replace(sql, @"\r?\n\s*\r?\n", "\n");

        return sql.Trim();
    }

    #region 字符串保护

    /// <summary>
    /// 将 SQL 中的字符串替换为占位符，避免转换时误替换字符串内容
    /// </summary>
    private string ProtectStrings(string sql)
    {
        // 匹配单引号字符串（包括转义的单引号 ''）
        var result = Regex.Replace(sql, @"'([^']*(?:'')*[^']*)'|''", match =>
        {
            var placeholder = $"__STRING_PLACEHOLDER_{_placeholderCounter}__";
            _stringPlaceholders[_placeholderCounter] = match.Value;
            _placeholderCounter++;
            return placeholder;
        });

        // 匹配双引号字符串（标识符）
        result = Regex.Replace(result, "\"([^\"]*)\"", match =>
        {
            var placeholder = $"__QUOTED_ID_{_placeholderCounter}__";
            _stringPlaceholders[_placeholderCounter] = match.Value;
            _placeholderCounter++;
            return placeholder;
        });

        return result;
    }

    /// <summary>
    /// 恢复被保护的字符串内容
    /// </summary>
    private string RestoreStrings(string sql)
    {
        // 恢复字符串
        foreach (var kvp in _stringPlaceholders)
        {
            sql = sql.Replace($"__STRING_PLACEHOLDER_{kvp.Key}__", kvp.Value);
            sql = sql.Replace($"__QUOTED_ID_{kvp.Key}__", kvp.Value);
        }
        return sql;
    }

    #endregion

    private string ConvertDataTypes(string sql)
    {
        // NVARCHAR -> NVARCHAR2
        sql = Regex.Replace(sql, @"\bNVARCHAR\b", "NVARCHAR2", RegexOptions.IgnoreCase);

        // VARCHAR -> VARCHAR2
        sql = Regex.Replace(sql, @"\bVARCHAR\b", "VARCHAR2", RegexOptions.IgnoreCase);

        // DATETIME2 -> TIMESTAMP
        sql = Regex.Replace(sql, @"\bDATETIME2\b", "TIMESTAMP", RegexOptions.IgnoreCase);

        // SMALLDATETIME -> TIMESTAMP
        sql = Regex.Replace(sql, @"\bSMALLDATETIME\b", "TIMESTAMP", RegexOptions.IgnoreCase);

        // DATETIME -> TIMESTAMP
        sql = Regex.Replace(sql, @"(?<!\w)DATETIME(?!\w|2)", "TIMESTAMP", RegexOptions.IgnoreCase);

        // SMALLMONEY -> DECIMAL(10,4)
        sql = Regex.Replace(sql, @"\bSMALLMONEY\b", "DECIMAL(10,4)", RegexOptions.IgnoreCase);

        // MONEY -> DECIMAL(19,4)
        sql = Regex.Replace(sql, @"(?<!\w)MONEY(?!\w)", "DECIMAL(19,4)", RegexOptions.IgnoreCase);

        // BIT -> TINYINT
        sql = Regex.Replace(sql, @"\bBIT\b", "TINYINT", RegexOptions.IgnoreCase);

        // UNIQUEIDENTIFIER -> VARCHAR2(36)
        sql = Regex.Replace(sql, @"\bUNIQUEIDENTIFIER\b", "VARCHAR2(36)", RegexOptions.IgnoreCase);

        // IMAGE -> BLOB
        sql = Regex.Replace(sql, @"\bIMAGE\b", "BLOB", RegexOptions.IgnoreCase);

        // VARBINARY(MAX) -> BLOB
        sql = Regex.Replace(sql, @"\bVARBINARY\s*\(\s*MAX\s*\)", "BLOB", RegexOptions.IgnoreCase);

        // VARBINARY(n) -> RAW(n)
        sql = Regex.Replace(sql, @"\bVARBINARY\s*\((\w+)\)", "RAW($1)", RegexOptions.IgnoreCase);

        // NVARCHAR(MAX) -> CLOB
        sql = Regex.Replace(sql, @"\bNVARCHAR2?\s*\(\s*MAX\s*\)", "CLOB", RegexOptions.IgnoreCase);

        // VARCHAR(MAX) -> CLOB
        sql = Regex.Replace(sql, @"\bVARCHAR2?\s*\(\s*MAX\s*\)", "CLOB", RegexOptions.IgnoreCase);

        // NTEXT -> TEXT
        sql = Regex.Replace(sql, @"\bNTEXT\b", "TEXT", RegexOptions.IgnoreCase);

        // REAL -> FLOAT
        sql = Regex.Replace(sql, @"(?<!\w)REAL(?!\w)", "FLOAT", RegexOptions.IgnoreCase);

        // FLOAT(n) - 根据精度转换
        sql = Regex.Replace(sql, @"FLOAT\s*\(\s*(\d+)\s*\)", match =>
        {
            var precision = int.Parse(match.Groups[1].Value);
            return precision <= 24 ? "FLOAT" : "DOUBLE";
        }, RegexOptions.IgnoreCase);

        // FLOAT -> DOUBLE
        sql = Regex.Replace(sql, @"(?<!\w)FLOAT(?!\w|\()", "DOUBLE", RegexOptions.IgnoreCase);

        // ROWVERSION -> RAW(8)
        sql = Regex.Replace(sql, @"\bROWVERSION\b", "RAW(8)", RegexOptions.IgnoreCase);

        return sql;
    }

    private string ConvertBuiltInFunctions(string sql)
    {
        // GETDATE() -> SYSDATE
        sql = Regex.Replace(sql, @"GETDATE\s*\(\s*\)", "SYSDATE", RegexOptions.IgnoreCase);

        // GETUTCDATE() -> SYS_EXTRACT_UTC(SYSDATE)
        sql = Regex.Replace(sql, @"GETUTCDATE\s*\(\s*\)", "SYS_EXTRACT_UTC(SYSDATE)", RegexOptions.IgnoreCase);

        // ISNULL(a, b) -> NVL(a, b)
        sql = Regex.Replace(sql, @"ISNULL\s*\(", "NVL(", RegexOptions.IgnoreCase);

        // LEN(x) -> LENGTH(x)
        sql = Regex.Replace(sql, @"\bLEN\s*\(", "LENGTH(", RegexOptions.IgnoreCase);

        // CHARINDEX(a, b) -> INSTR(b, a) - 需要处理参数交换
        sql = ConvertCharIndex(sql);

        // LEFT(s, n) -> SUBSTR(s, 1, n)
        sql = Regex.Replace(sql, @"\bLEFT\s*\(([^,]+),\s*([^)]+)\)", "SUBSTR($1, 1, $2)", RegexOptions.IgnoreCase);

        // RIGHT(s, n) -> SUBSTR(s, -n)
        sql = Regex.Replace(sql, @"\bRIGHT\s*\(([^,]+),\s*([^)]+)\)", "SUBSTR($1, -$2)", RegexOptions.IgnoreCase);

        // SUBSTRING -> SUBSTR
        sql = Regex.Replace(sql, @"\bSUBSTRING\s*\(", "SUBSTR(", RegexOptions.IgnoreCase);

        // REPLICATE(s, n) -> RPAD
        sql = Regex.Replace(sql, @"\bREPLICATE\s*\(([^,]+),\s*([^)]+)\)", "RPAD(' ', LENGTH($1) * $2, $1)", RegexOptions.IgnoreCase);

        // STUFF 转换
        sql = ConvertStuff(sql);

        // DATEPART 转换
        sql = ConvertDatePart(sql);

        // DATEDIFF 转换
        sql = ConvertDateDiff(sql);

        // DATEADD 转换
        sql = ConvertDateAdd(sql);

        // YEAR/MONTH/DAY -> EXTRACT
        sql = Regex.Replace(sql, @"YEAR\s*\(([^)]+)\)", "EXTRACT(YEAR FROM $1)", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"MONTH\s*\(([^)]+)\)", "EXTRACT(MONTH FROM $1)", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"DAY\s*\(([^)]+)\)", "EXTRACT(DAY FROM $1)", RegexOptions.IgnoreCase);

        // CONVERT(type, expr) -> CAST(expr AS type)
        sql = ConvertConvertToCast(sql);

        // NEWID() -> SYS_GUID()
        sql = Regex.Replace(sql, @"NEWID\s*\(\s*\)", "SYS_GUID()", RegexOptions.IgnoreCase);

        // SCOPE_IDENTITY() -> IDENTITY()
        sql = Regex.Replace(sql, @"SCOPE_IDENTITY\s*\(\s*\)", "IDENTITY()", RegexOptions.IgnoreCase);

        // @@IDENTITY -> IDENTITY()
        sql = Regex.Replace(sql, @"@@IDENTITY", "IDENTITY()", RegexOptions.IgnoreCase);

        // @@ERROR -> SQLCODE
        sql = Regex.Replace(sql, @"@@ERROR", "SQLCODE", RegexOptions.IgnoreCase);

        // @@ROWCOUNT -> SQL%ROWCOUNT
        sql = Regex.Replace(sql, @"@@ROWCOUNT", "SQL%ROWCOUNT", RegexOptions.IgnoreCase);

        // DB_NAME() -> SYS_CONTEXT('USERENV','DB_NAME')
        sql = Regex.Replace(sql, @"DB_NAME\s*\(\s*\)", "SYS_CONTEXT('USERENV','DB_NAME')", RegexOptions.IgnoreCase);

        // HOST_NAME() -> SYS_CONTEXT('USERENV','HOST')
        sql = Regex.Replace(sql, @"HOST_NAME\s*\(\s*\)", "SYS_CONTEXT('USERENV','HOST')", RegexOptions.IgnoreCase);

        // SUSER_NAME() -> SYS_CONTEXT('USERENV','SESSION_USER')
        sql = Regex.Replace(sql, @"SUSER_NAME\s*\(\s*\)", "SYS_CONTEXT('USERENV','SESSION_USER')", RegexOptions.IgnoreCase);

        // USER_NAME() -> SYS_CONTEXT('USERENV','SESSION_USER')
        sql = Regex.Replace(sql, @"USER_NAME\s*\(\s*\)", "SYS_CONTEXT('USERENV','SESSION_USER')", RegexOptions.IgnoreCase);

        // STRING_AGG(col, sep) -> LISTAGG(col, sep)
        sql = Regex.Replace(sql, @"STRING_AGG\s*\(\s*([^,]+),\s*([^)]+)\s*\)", "LISTAGG($1, $2)", RegexOptions.IgnoreCase);

        // IIF(condition, true, false) -> CASE WHEN condition THEN true ELSE false END
        sql = ConvertIIF(sql);

        // CHOOSE -> 需要手动处理
        if (Regex.IsMatch(sql, @"\bCHOOSE\s*\(", RegexOptions.IgnoreCase))
        {
            _warnings.Add(new ConvertWarning
            {
                Message = "CHOOSE 函数需要手动转换为 DECODE 或 CASE 语句",
                Severity = WarningSeverity.Warning
            });
        }

        return sql;
    }

    private string ConvertDateFunctions(string sql)
    {
        sql = ConvertDateAdd(sql);
        sql = ConvertDateDiff(sql);
        return sql;
    }

    private string ConvertDateAdd(string sql)
    {
        var pattern = @"DATEADD\s*\(\s*(YEAR|YY|YYYY|QUARTER|QQ|MONTH|MM|M|DAYOFYEAR|DY|DAY|DD|D|WEEK|WK|WW|HOUR|HH|MINUTE|MI|N|SECOND|SS|S|MILLISECOND|MS)\s*,\s*([^,]+),\s*([^)]+)\s*\)";

        return Regex.Replace(sql, pattern, match =>
        {
            var part = match.Groups[1].Value.ToUpper();
            var n = match.Groups[2].Value.Trim();
            var d = match.Groups[3].Value.Trim();

            return part switch
            {
                "YEAR" or "YY" or "YYYY" => $"ADD_MONTHS({d}, {n}*12)",
                "QUARTER" or "QQ" => $"ADD_MONTHS({d}, {n}*3)",
                "MONTH" or "MM" or "M" => $"ADD_MONTHS({d}, {n})",
                "DAYOFYEAR" or "DY" or "DAY" or "DD" or "D" => $"{d} + {n}",
                "WEEK" or "WK" or "WW" => $"{d} + {n}*7",
                "HOUR" or "HH" => $"{d} + {n}/24",
                "MINUTE" or "MI" or "N" => $"{d} + {n}/1440",
                "SECOND" or "SS" or "S" => $"{d} + {n}/86400",
                "MILLISECOND" or "MS" => HandleMillisecondAdd(n, d),
                _ => match.Value
            };
        }, RegexOptions.IgnoreCase);
    }

    private string HandleMillisecondAdd(string n, string d)
    {
        _warnings.Add(new ConvertWarning
        {
            Message = "DATEADD(MILLISECOND, ...) 需要手动处理，DM8 使用不同的精度",
            Severity = WarningSeverity.Warning
        });
        return $"{d} + {n}/86400000";
    }

    private string ConvertDateDiff(string sql)
    {
        var pattern = @"DATEDIFF\s*\(\s*(YEAR|YY|YYYY|QUARTER|QQ|MONTH|MM|M|DAYOFYEAR|DY|DAY|DD|D|WEEK|WK|WW|HOUR|HH|MINUTE|MI|N|SECOND|SS|S|MILLISECOND|MS)\s*,\s*([^,]+),\s*([^)]+)\s*\)";

        return Regex.Replace(sql, pattern, match =>
        {
            var part = match.Groups[1].Value.ToUpper();
            var a = match.Groups[2].Value.Trim();
            var b = match.Groups[3].Value.Trim();

            return part switch
            {
                "YEAR" or "YY" or "YYYY" => $"EXTRACT(YEAR FROM {b}) - EXTRACT(YEAR FROM {a})",
                "QUARTER" or "QQ" => $"(EXTRACT(YEAR FROM {b}) - EXTRACT(YEAR FROM {a})) * 3 + (EXTRACT(MONTH FROM {b}) - EXTRACT(MONTH FROM {a})) / 3",
                "MONTH" or "MM" or "M" => $"MONTHS_BETWEEN({b}, {a})",
                "DAYOFYEAR" or "DY" or "DAY" or "DD" or "D" => $"TRUNC({b}) - TRUNC({a})",
                "WEEK" or "WK" or "WW" => $"(TRUNC({b}) - TRUNC({a})) / 7",
                "HOUR" or "HH" => $"({b} - {a}) * 24",
                "MINUTE" or "MI" or "N" => $"({b} - {a}) * 1440",
                "SECOND" or "SS" or "S" => $"({b} - {a}) * 86400",
                "MILLISECOND" or "MS" => HandleMillisecondDiff(a, b),
                _ => match.Value
            };
        }, RegexOptions.IgnoreCase);
    }

    private string HandleMillisecondDiff(string a, string b)
    {
        _warnings.Add(new ConvertWarning
        {
            Message = "DATEDIFF(MILLISECOND, ...) 需要手动处理，DM8 使用不同的精度",
            Severity = WarningSeverity.Warning
        });
        return $"({b} - {a}) * 86400000";
    }

    private string ConvertCharIndex(string sql)
    {
        var pattern = @"CHARINDEX\s*\(\s*([^,]+),\s*([^,)]+)(?:,\s*([^)]+))?\s*\)";
        return Regex.Replace(sql, pattern, match =>
        {
            var first = match.Groups[1].Value.Trim();
            var second = match.Groups[2].Value.Trim();
            if (match.Groups[3].Success)
            {
                var third = match.Groups[3].Value.Trim();
                return $"INSTR({second}, {first}, {third})";
            }
            return $"INSTR({second}, {first})";
        }, RegexOptions.IgnoreCase);
    }

    private string ConvertDatePart(string sql)
    {
        var pattern = @"DATEPART\s*\(\s*(YEAR|YY|YYYY|QUARTER|QQ|MONTH|MM|M|DAYOFYEAR|DY|DAY|DD|D|WEEK|WK|WW|WEEKDAY|DW|HOUR|HH|MINUTE|MI|N|SECOND|SS|S|MILLISECOND|MS)\s*,\s*([^)]+)\s*\)";
        return Regex.Replace(sql, pattern, match =>
        {
            var part = match.Groups[1].Value.ToUpper();
            var dateExpr = match.Groups[2].Value.Trim();

            var dm8Part = part switch
            {
                "YEAR" or "YY" or "YYYY" => "YEAR",
                "QUARTER" or "QQ" => "QUARTER",
                "MONTH" or "MM" or "M" => "MONTH",
                "DAYOFYEAR" or "DY" => "DOY",
                "DAY" or "DD" or "D" => "DAY",
                "WEEK" or "WK" or "WW" => "WEEK",
                "WEEKDAY" or "DW" => "DOW",
                "HOUR" or "HH" => "HOUR",
                "MINUTE" or "MI" or "N" => "MINUTE",
                "SECOND" or "SS" or "S" => "SECOND",
                "MILLISECOND" or "MS" => null,
                _ => null
            };

            if (dm8Part == null)
            {
                _warnings.Add(new ConvertWarning
                {
                    Message = $"DATEPART({part}, ...) 不支持直接转换，需要手动处理",
                    Severity = WarningSeverity.Warning
                });
                return match.Value;
            }

            return $"EXTRACT({dm8Part} FROM {dateExpr})";
        }, RegexOptions.IgnoreCase);
    }

    private string ConvertConvertToCast(string sql)
    {
        // 处理带样式的 CONVERT(type, expr, style) -> TO_CHAR/TO_DATE
        var stylePattern = @"CONVERT\s*\(\s*([^,]+),\s*([^,]+),\s*(\d+)\s*\)";
        sql = Regex.Replace(sql, stylePattern, match =>
        {
            var type = match.Groups[1].Value.Trim();
            var expr = match.Groups[2].Value.Trim();
            var style = int.Parse(match.Groups[3].Value);

            // 根据目标类型和样式生成 DM8 等价表达式
            if (type.Contains("NVARCHAR", StringComparison.OrdinalIgnoreCase) || 
                type.Contains("VARCHAR", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("CHAR", StringComparison.OrdinalIgnoreCase))
            {
                // 日期转字符串
                var format = ConvertStyleToFormat(style);
                if (format != null)
                {
                    return $"TO_CHAR({expr}, '{format}')";
                }
                return $"CAST({expr} AS {type})";
            }
            else if (type.Contains("DATETIME", StringComparison.OrdinalIgnoreCase) || 
                     type.Contains("DATE", StringComparison.OrdinalIgnoreCase) ||
                     type.Contains("TIMESTAMP", StringComparison.OrdinalIgnoreCase))
            {
                // 字符串转日期
                var format = ConvertStyleToFormat(style);
                if (format != null)
                {
                    return $"TO_DATE({expr}, '{format}')";
                }
                return $"CAST({expr} AS TIMESTAMP)";
            }
            
            // 其他类型使用 CAST
            return $"CAST({expr} AS {type})";
        }, RegexOptions.IgnoreCase);

        // 处理简单的 CONVERT(type, expr)
        var simplePattern = @"CONVERT\s*\(\s*([^,]+),\s*([^,)]+)\s*\)";
        return Regex.Replace(sql, simplePattern, match =>
        {
            var type = match.Groups[1].Value.Trim();
            var expr = match.Groups[2].Value.Trim();
            return $"CAST({expr} AS {type})";
        }, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 将 SQL Server 的 CONVERT 样式号转换为 Oracle/DM8 的日期格式
    /// </summary>
    private string? ConvertStyleToFormat(int style)
    {
        return style switch
        {
            0 => "YYYY-MM-DD HH24:MI:SS",
            1 => "MM/DD/YY",
            2 => "YY.MM.DD",
            3 => "DD/MM/YY",
            4 => "DD.MM.YY",
            5 => "DD-MM-YY",
            6 => "DD Mon YY",
            7 => "Mon DD, YY",
            8 => "HH24:MI:SS",
            10 => "MM-DD-YY",
            11 => "YY/MM/DD",
            12 => "YYMMDD",
            13 => "DD Mon YYYY HH24:MI:SS",
            14 => "HH24:MI:SS:FF3",
            20 => "YYYY-MM-DD HH24:MI:SS",
            21 => "YYYY-MM-DD HH24:MI:SS",
            101 => "MM/DD/YYYY",
            102 => "YYYY.MM.DD",
            103 => "DD/MM/YYYY",
            104 => "DD.MM.YYYY",
            105 => "DD-MM-YYYY",
            106 => "DD Mon YYYY",
            107 => "Mon DD, YYYY",
            108 => "HH24:MI:SS",
            110 => "MM-DD-YYYY",
            111 => "YYYY/MM/DD",
            112 => "YYYYMMDD",
            120 => "YYYY-MM-DD HH24:MI:SS",
            121 => "YYYY-MM-DD HH24:MI:SS",
            126 => "YYYY-MM-DD\"T\"HH24:MI:SS",
            127 => "YYYY-MM-DD\"T\"HH24:MI:SS",
            130 => "DD Mon YYYY HH24:MI:SS",
            131 => "DD/MM/YYYY HH24:MI:SS",
            _ => null
        };
    }

    private string ConvertStuff(string sql)
    {
        var pattern = @"\bSTUFF\s*\(\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*([^)]+)\s*\)";
        return Regex.Replace(sql, pattern, match =>
        {
            var s = match.Groups[1].Value.Trim();
            var start = match.Groups[2].Value.Trim();
            var length = match.Groups[3].Value.Trim();
            var replace = match.Groups[4].Value.Trim();

            return $"SUBSTR({s}, 1, {start}-1) || {replace} || SUBSTR({s}, {start}+{length})";
        }, RegexOptions.IgnoreCase);
    }

    private string ConvertIIF(string sql)
    {
        var pattern = @"\bIIF\s*\(\s*([^,]+),\s*([^,]+),\s*([^)]+)\s*\)";
        return Regex.Replace(sql, pattern, match =>
        {
            var condition = match.Groups[1].Value.Trim();
            var trueValue = match.Groups[2].Value.Trim();
            var falseValue = match.Groups[3].Value.Trim();

            return $"CASE WHEN {condition} THEN {trueValue} ELSE {falseValue} END";
        }, RegexOptions.IgnoreCase);
    }

    private string ConvertSyntax(string sql, DatabaseObjectType objectType)
    {
        // 游标转换（必须在其他转换之前）
        sql = ConvertCursor(sql);

        // 临时表转换
        sql = ConvertTempTables(sql);

        // SELECT TOP N -> FETCH FIRST N ROWS ONLY
        sql = ConvertTopN(sql);

        // 方括号 -> 双引号
        sql = ConvertBrackets(sql);

        // += 字符串拼接 -> ||
        sql = Regex.Replace(sql, @"\+=", "||", RegexOptions.IgnoreCase);

        // OUTER APPLY -> LEFT JOIN LATERAL
        sql = Regex.Replace(sql, @"\bOUTER\s+APPLY\b", "LEFT JOIN LATERAL", RegexOptions.IgnoreCase);

        // CROSS APPLY -> CROSS JOIN LATERAL
        sql = Regex.Replace(sql, @"\bCROSS\s+APPLY\b", "CROSS JOIN LATERAL", RegexOptions.IgnoreCase);

        // 存储过程/函数语法转换
        if (objectType == DatabaseObjectType.Procedure || objectType == DatabaseObjectType.Function)
        {
            sql = ConvertProcedureSyntax(sql, objectType);
        }

        // CREATE 语句模板转换
        sql = ConvertCreateStatement(sql, objectType);

        return sql;
    }

    #region 游标转换

    private string ConvertCursor(string sql)
    {
        // 匹配完整的游标使用模式
        // DECLARE cursor_name CURSOR FOR SELECT ...
        // OPEN cursor_name
        // FETCH NEXT FROM cursor_name INTO @var1, @var2
        // WHILE @@FETCH_STATUS = 0
        // BEGIN
        //     ...
        //     FETCH NEXT FROM cursor_name INTO @var1, @var2
        // END
        // CLOSE cursor_name
        // DEALLOCATE cursor_name

        // 简化处理：逐个转换游标语句
        // 1. DECLARE xxx CURSOR FOR -> 添加注释提示
        sql = Regex.Replace(sql, @"DECLARE\s+(\w+)\s+CURSOR\s+FOR\s+", match =>
        {
            var cursorName = match.Groups[1].Value;
            _warnings.Add(new ConvertWarning
            {
                Message = $"游标 {cursorName} 需要转换为 FOR LOOP 或 WHILE LOOP",
                Severity = WarningSeverity.Warning
            });
            return $"-- 游标 {cursorName} 需要转换为循环\n-- DECLARE {cursorName} CURSOR FOR ";
        }, RegexOptions.IgnoreCase);

        // 2. OPEN cursor_name -> 注释
        sql = Regex.Replace(sql, @"\bOPEN\s+(\w+)\s*;?", "-- OPEN $1 (DM8 FOR LOOP 自动打开)", RegexOptions.IgnoreCase);

        // 3. FETCH NEXT FROM cursor_name INTO @var -> 注释
        sql = Regex.Replace(sql, @"FETCH\s+NEXT\s+FROM\s+(\w+)\s+INTO\s+", match =>
        {
            return $"-- FETCH {match.Groups[1].Value} INTO (在 FOR LOOP 中自动获取)";
        }, RegexOptions.IgnoreCase);

        // 4. WHILE @@FETCH_STATUS = 0 -> LOOP (在 ConvertWhileLoop 中处理)
        sql = Regex.Replace(sql, @"WHILE\s+@@FETCH_STATUS\s*=\s*0", "LOOP -- 原: WHILE @@FETCH_STATUS = 0", RegexOptions.IgnoreCase);

        // 5. CLOSE cursor_name -> 注释
        sql = Regex.Replace(sql, @"\bCLOSE\s+(\w+)\s*;?", "-- CLOSE $1 (DM8 FOR LOOP 自动关闭)", RegexOptions.IgnoreCase);

        // 6. DEALLOCATE cursor_name -> 注释
        sql = Regex.Replace(sql, @"\bDEALLOCATE\s+(\w+)\s*;?", "-- DEALLOCATE $1 (DM8 无需手动释放)", RegexOptions.IgnoreCase);

        return sql;
    }

    #endregion

    #region 临时表转换

    private string ConvertTempTables(string sql)
    {
        // #temp -> TEMP_前缀的全局临时表
        // CREATE TABLE #temp -> CREATE GLOBAL TEMPORARY TABLE TEMP_temp
        sql = Regex.Replace(sql, @"CREATE\s+TABLE\s+#(\w+)", match =>
        {
            var tableName = match.Groups[1].Value;
            _warnings.Add(new ConvertWarning
            {
                Message = $"临时表 #{tableName} 已转换为全局临时表 TEMP_{tableName}，请确认表结构兼容",
                Severity = WarningSeverity.Warning
            });
            return $"CREATE GLOBAL TEMPORARY TABLE TEMP_{tableName}";
        }, RegexOptions.IgnoreCase);

        // SELECT ... INTO #temp -> 创建临时表
        sql = Regex.Replace(sql, @"INTO\s+#(\w+)", match =>
        {
            var tableName = match.Groups[1].Value;
            return $"INTO TEMP_{tableName}";
        }, RegexOptions.IgnoreCase);

        // 引用临时表 #temp -> TEMP_temp
        sql = Regex.Replace(sql, @"(?<!\w)#(\w+)", match =>
        {
            // 避免替换注释中的 #
            var prefix = match.Value;
            var tableName = match.Groups[1].Value;
            return $"TEMP_{tableName}";
        }, RegexOptions.IgnoreCase);

        // DROP TABLE #temp -> DROP TABLE TEMP_temp
        sql = Regex.Replace(sql, @"DROP\s+TABLE\s+TEMP_(\w+)", "DROP TABLE TEMP_$1", RegexOptions.IgnoreCase);

        return sql;
    }

    #endregion

    #region 表变量转换

    private string ConvertTableVariables(string sql)
    {
        // DECLARE @table TABLE (col1 INT, col2 VARCHAR(50))
        // -> 声明为 PL/SQL 集合类型或使用全局临时表

        // 匹配 DECLARE @table TABLE (...)
        var pattern = @"DECLARE\s+@(\w+)\s+TABLE\s*\(([^)]+)\)";
        sql = Regex.Replace(sql, pattern, match =>
        {
            var varName = match.Groups[1].Value;
            var columns = match.Groups[2].Value.Trim();

            _warnings.Add(new ConvertWarning
            {
                Message = $"表变量 @{varName} 需要转换为 PL/SQL 集合类型或全局临时表",
                Severity = WarningSeverity.Warning
            });

            // 转换为全局临时表
            var columnsConverted = ConvertTableVariableColumns(columns);
            return $"-- 表变量 @{varName} 转换为全局临时表\nCREATE GLOBAL TEMPORARY TABLE TV_{varName} ({columnsConverted})";
        }, RegexOptions.IgnoreCase);

        return sql;
    }

    private string ConvertTableVariableColumns(string columns)
    {
        // 转换列定义中的数据类型
        var result = columns;
        result = Regex.Replace(result, @"\bNVARCHAR\b", "NVARCHAR2", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bVARCHAR\b", "VARCHAR2", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bDATETIME\b", "TIMESTAMP", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bBIT\b", "TINYINT", RegexOptions.IgnoreCase);
        return result;
    }

    #endregion

    #region TOP N 转换

    private string ConvertTopN(string sql)
    {
        var selectPattern = @"(SELECT\s+)(TOP\s+\(?\s*(\d+)\s*\)?\s+)(.*?)($|;|\s+UNION|\s+INTERSECT|\s+EXCEPT|\s+ORDER\s+BY|\s+GROUP\s+BY|\s+HAVING|\s+WHERE)";
        return Regex.Replace(sql, selectPattern, match =>
        {
            var selectKeyword = match.Groups[1].Value;
            var n = match.Groups[3].Value;
            var rest = match.Groups[4].Value;
            var terminator = match.Groups[5].Value;

            var newSql = $"{selectKeyword}{rest}";

            if (!newSql.Contains("FETCH FIRST", StringComparison.OrdinalIgnoreCase))
            {
                newSql = $"{newSql} FETCH FIRST {n} ROWS ONLY";
            }

            return $"{newSql}{terminator}";
        }, RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    #endregion

    private string ConvertBrackets(string sql)
    {
        return Regex.Replace(sql, @"\[([^\]]+)\]", "\"$1\"");
    }

    #region 存储过程语法转换

    private string ConvertProcedureSyntax(string sql, DatabaseObjectType objectType)
    {
        // 处理 TRY...CATCH（必须在其他转换之前）
        sql = ConvertTryCatch(sql);

        // 游标循环处理
        sql = ConvertCursorLoop(sql);

        // 表变量转换
        sql = ConvertTableVariables(sql);

        // IF ... BEGIN ... END -> IF ... THEN ... END IF;
        sql = ConvertIfBlock(sql);

        // ELSE IF -> ELSIF
        sql = Regex.Replace(sql, @"\bELSE\s+IF\b", "ELSIF", RegexOptions.IgnoreCase);

        // WHILE ... BEGIN ... END -> WHILE ... LOOP ... END LOOP;
        sql = ConvertWhileLoop(sql);

        // SET @var = value -> var := value;
        sql = Regex.Replace(sql, @"SET\s+@(\w+)\s*=", "$1 :=", RegexOptions.IgnoreCase);

        // DECLARE @var TYPE -> var TYPE;
        sql = ConvertDeclare(sql);

        // PRINT -> DBMS_OUTPUT.PUT_LINE
        sql = Regex.Replace(sql, @"\bPRINT\s+([^;]+);?", "DBMS_OUTPUT.PUT_LINE($1);", RegexOptions.IgnoreCase);

        // RAISERROR -> RAISE_APPLICATION_ERROR
        sql = ConvertRaiserror(sql);

        // EXEC/EXECUTE 动态 SQL
        sql = ConvertExec(sql);

        // 事务处理提示
        sql = ConvertTransaction(sql);

        return sql;
    }

    #endregion

    #region TRY...CATCH 转换

    private string ConvertTryCatch(string sql)
    {
        // 匹配完整的 TRY...CATCH 块
        // BEGIN TRY ... END TRY BEGIN CATCH ... END CATCH
        // 转换为:
        // BEGIN
        //     ... (try 代码)
        // EXCEPTION
        //     WHEN OTHERS THEN
        //         ... (catch 代码)
        // END;

        // 先移除 BEGIN TRY 和 END TRY
        sql = Regex.Replace(sql, @"\bBEGIN\s+TRY\b", "", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bEND\s+TRY\b", "", RegexOptions.IgnoreCase);

        // BEGIN CATCH -> EXCEPTION WHEN OTHERS THEN
        sql = Regex.Replace(sql, @"\bBEGIN\s+CATCH\b", 
            "EXCEPTION\n    WHEN OTHERS THEN", RegexOptions.IgnoreCase);

        // END CATCH -> END (添加注释)
        sql = Regex.Replace(sql, @"\bEND\s+CATCH\b", 
            "-- END CATCH (DM8 的 EXCEPTION 块自动结束)", RegexOptions.IgnoreCase);

        return sql;
    }

    #endregion

    #region 游标循环转换

    private string ConvertCursorLoop(string sql)
    {
        // 将 WHILE @@FETCH_STATUS = 0 ... END 转换为 LOOP ... END LOOP
        // 这是一个简化实现，完整实现需要更复杂的解析

        // 添加提示
        if (sql.Contains("@@FETCH_STATUS", StringComparison.OrdinalIgnoreCase))
        {
            _warnings.Add(new ConvertWarning
            {
                Message = "游标循环需要手动调整为 DM8 的 FOR LOOP 或 WHILE LOOP 语法",
                Severity = WarningSeverity.Warning
            });
        }

        return sql;
    }

    #endregion

    #region IF 块转换

    private string ConvertIfBlock(string sql)
    {
        // IF ... BEGIN ... END -> IF ... THEN ... END IF;
        // 处理嵌套情况

        // 简单的 IF ... BEGIN ... END
        sql = Regex.Replace(sql, @"\bIF\s+(.+?)\s+BEGIN\b", "IF $1 THEN", RegexOptions.IgnoreCase);

        // 处理 ELSE BEGIN -> ELSE
        sql = Regex.Replace(sql, @"\bELSE\s+BEGIN\b", "ELSE", RegexOptions.IgnoreCase);

        return sql;
    }

    #endregion

    #region WHILE 循环转换

    private string ConvertWhileLoop(string sql)
    {
        // WHILE ... BEGIN ... END -> WHILE ... LOOP ... END LOOP;
        // 注意：需要正确处理嵌套和 END 的匹配

        sql = Regex.Replace(sql, @"\bWHILE\s+(.+?)\s+BEGIN\b", match =>
        {
            _loopDepth++;
            return $"WHILE {match.Groups[1].Value.Trim()} LOOP";
        }, RegexOptions.IgnoreCase);

        // END -> END LOOP; 只在循环深度 > 0 时转换
        // 这是一个简化实现，实际需要更复杂的解析来正确匹配 END
        // 暂时保留原样，让用户手动调整

        return sql;
    }

    #endregion

    #region DECLARE 转换

    private string ConvertDeclare(string sql)
    {
        // DECLARE @var TYPE [DEFAULT value] -> var TYPE [DEFAULT value];
        var pattern = @"DECLARE\s+(@\w+)\s+(\w+(?:\s*\([^)]*\))?)(?:\s+DEFAULT\s+([^;]+))?";
        return Regex.Replace(sql, pattern, match =>
        {
            var varName = match.Groups[1].Value.TrimStart('@');
            var type = match.Groups[2].Value;
            var defaultValue = match.Groups[3].Success ? $" DEFAULT {match.Groups[3].Value.Trim()}" : "";

            type = ConvertDataTypeInDeclare(type);

            return $"{varName} {type}{defaultValue};";
        }, RegexOptions.IgnoreCase);
    }

    private string ConvertDataTypeInDeclare(string type)
    {
        type = Regex.Replace(type, @"\bNVARCHAR\b", "NVARCHAR2", RegexOptions.IgnoreCase);
        type = Regex.Replace(type, @"\bVARCHAR\b", "VARCHAR2", RegexOptions.IgnoreCase);
        type = Regex.Replace(type, @"\bDATETIME\b", "TIMESTAMP", RegexOptions.IgnoreCase);
        type = Regex.Replace(type, @"\bBIT\b", "TINYINT", RegexOptions.IgnoreCase);
        return type;
    }

    #endregion

    #region RAISERROR 转换

    private string ConvertRaiserror(string sql)
    {
        // RAISERROR('msg', severity, state) -> RAISE_APPLICATION_ERROR(-20000, 'msg')
        // 简化处理：只保留消息文本
        sql = Regex.Replace(sql, @"RAISERROR\s*\(\s*'([^']+)'\s*,\s*\d+\s*,\s*\d+\s*\)",
            "RAISE_APPLICATION_ERROR(-20000, '$1')", RegexOptions.IgnoreCase);

        // 处理变量形式的 RAISERROR
        if (Regex.IsMatch(sql, @"RAISERROR\s*\(\s*@", RegexOptions.IgnoreCase))
        {
            _warnings.Add(new ConvertWarning
            {
                Message = "RAISERROR 使用变量参数，需要手动转换为 RAISE_APPLICATION_ERROR",
                Severity = WarningSeverity.Warning
            });
        }

        return sql;
    }

    #endregion

    #region EXEC 动态 SQL 转换

    private string ConvertExec(string sql)
    {
        // EXEC(@sql) -> EXECUTE IMMEDIATE @sql;
        sql = Regex.Replace(sql, @"EXEC\s*\(\s*@(\w+)\s*\)", "EXECUTE IMMEDIATE $1;", RegexOptions.IgnoreCase);

        // EXEC sp_name params -> sp_name params;
        // 但要避免误替换 EXECUTE IMMEDIATE
        sql = Regex.Replace(sql, @"\bEXEC\s+(?!IMMEDIATE)(\w+)", "$1", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bEXECUTE\s+(?!IMMEDIATE)(\w+)", "$1", RegexOptions.IgnoreCase);

        return sql;
    }

    #endregion

    #region 事务处理转换

    private string ConvertTransaction(string sql)
    {
        // BEGIN TRANSACTION -> BEGIN (DM8 支持)
        // COMMIT TRANSACTION -> COMMIT
        // ROLLBACK TRANSACTION -> ROLLBACK

        // SQL Server 和 DM8 的事务处理基本兼容，添加注释提示
        if (Regex.IsMatch(sql, @"\bBEGIN\s+TRANSACTION\b", RegexOptions.IgnoreCase))
        {
            _warnings.Add(new ConvertWarning
            {
                Message = "事务处理语法已转换，DM8 使用隐式事务或 SAVEPOINT，请确认业务逻辑",
                Severity = WarningSeverity.Info
            });
        }

        // BEGIN TRANSACTION -> BEGIN
        sql = Regex.Replace(sql, @"\bBEGIN\s+TRAN(SACTION)?\b", "BEGIN", RegexOptions.IgnoreCase);

        // COMMIT TRANSACTION -> COMMIT
        sql = Regex.Replace(sql, @"\bCOMMIT\s+TRAN(SACTION)?\b", "COMMIT", RegexOptions.IgnoreCase);

        // ROLLBACK TRANSACTION -> ROLLBACK
        sql = Regex.Replace(sql, @"\bROLLBACK\s+TRAN(SACTION)?\b", "ROLLBACK", RegexOptions.IgnoreCase);

        // SAVE TRANSACTION -> SAVEPOINT
        sql = Regex.Replace(sql, @"\bSAVE\s+TRAN(SACTION)?\s+(\w+)", "SAVEPOINT $2", RegexOptions.IgnoreCase);

        return sql;
    }

    #endregion

    #region CREATE 语句转换

    private string ConvertCreateStatement(string sql, DatabaseObjectType objectType)
    {
        switch (objectType)
        {
            case DatabaseObjectType.View:
                sql = Regex.Replace(sql, @"CREATE\s+VIEW\b", "CREATE OR REPLACE VIEW", RegexOptions.IgnoreCase);
                break;

            case DatabaseObjectType.Procedure:
                sql = Regex.Replace(sql, @"CREATE\s+(PROCEDURE|PROC)\b", "CREATE OR REPLACE PROCEDURE", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\bAS\s+BEGIN\b", "AS\nBEGIN", RegexOptions.IgnoreCase);
                sql = ConvertProcedureParameters(sql);
                break;

            case DatabaseObjectType.Function:
                sql = Regex.Replace(sql, @"CREATE\s+FUNCTION\b", "CREATE OR REPLACE FUNCTION", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\bRETURNS\b", "RETURN", RegexOptions.IgnoreCase);
                sql = ConvertFunctionParameters(sql);
                break;
        }

        return sql;
    }

    private string ConvertProcedureParameters(string sql)
    {
        var paramPattern = @"(@\w+)\s+(INT|INTEGER|BIGINT|SMALLINT|TINYINT|DECIMAL|NUMERIC|FLOAT|DOUBLE|VARCHAR2?\s*\([^)]*\)|NVARCHAR2?\s*\([^)]*\)|CHAR|NCHAR|DATE|TIMESTAMP|BIT|TINYINT|MONEY|TEXT|NTEXT|BLOB|CLOB)(?:\s*(OUTPUT|OUT))?";

        return Regex.Replace(sql, paramPattern, match =>
        {
            var paramName = match.Groups[1].Value.TrimStart('@');
            var paramType = match.Groups[2].Value;
            var isOutput = match.Groups[3].Success;

            var direction = isOutput ? "OUT" : "IN";
            return $"{paramName} {direction} {paramType}";
        }, RegexOptions.IgnoreCase);
    }

    private string ConvertFunctionParameters(string sql)
    {
        var paramPattern = @"(@\w+)\s+(INT|INTEGER|BIGINT|SMALLINT|TINYINT|DECIMAL|NUMERIC|FLOAT|DOUBLE|VARCHAR2?\s*\([^)]*\)|NVARCHAR2?\s*\([^)]*\)|CHAR|NCHAR|DATE|TIMESTAMP|BIT|TINYINT|MONEY|TEXT|NTEXT|BLOB|CLOB)";

        return Regex.Replace(sql, paramPattern, match =>
        {
            var paramName = match.Groups[1].Value.TrimStart('@');
            var paramType = match.Groups[2].Value;

            return $"{paramName} {paramType}";
        }, RegexOptions.IgnoreCase);
    }

    #endregion

    #region 后处理

    private string PostProcess(string sql)
    {
        // 添加分号结尾（如果缺少）
        if (!sql.TrimEnd().EndsWith(";"))
        {
            sql = sql.TrimEnd() + ";";
        }

        // 移除多余的空行
        sql = Regex.Replace(sql, @"\n{3,}", "\n\n");

        // 清理多余的空格
        sql = Regex.Replace(sql, @" {2,}", " ");

        return sql.Trim();
    }

    #endregion

    private double CalculateConfidence(int warningCount)
    {
        if (warningCount == 0) return 1.0;
        return Math.Max(0.3, 1.0 - (warningCount * 0.1));
    }

    public List<ConvertRule> GetRules() => _rules;

    private List<ConvertRule> InitializeRules()
    {
        return new List<ConvertRule>
        {
            // 数据类型映射
            new() { Id = "DT-001", Category = "数据类型", Description = "NVARCHAR -> NVARCHAR2", SqlServerPattern = "NVARCHAR", DM8Replacement = "NVARCHAR2" },
            new() { Id = "DT-002", Category = "数据类型", Description = "VARCHAR -> VARCHAR2", SqlServerPattern = "VARCHAR", DM8Replacement = "VARCHAR2" },
            new() { Id = "DT-003", Category = "数据类型", Description = "DATETIME -> TIMESTAMP", SqlServerPattern = "DATETIME", DM8Replacement = "TIMESTAMP" },
            new() { Id = "DT-004", Category = "数据类型", Description = "DATETIME2 -> TIMESTAMP", SqlServerPattern = "DATETIME2", DM8Replacement = "TIMESTAMP" },
            new() { Id = "DT-005", Category = "数据类型", Description = "MONEY -> DECIMAL(19,4)", SqlServerPattern = "MONEY", DM8Replacement = "DECIMAL(19,4)" },
            new() { Id = "DT-006", Category = "数据类型", Description = "BIT -> TINYINT", SqlServerPattern = "BIT", DM8Replacement = "TINYINT" },
            new() { Id = "DT-007", Category = "数据类型", Description = "UNIQUEIDENTIFIER -> VARCHAR2(36)", SqlServerPattern = "UNIQUEIDENTIFIER", DM8Replacement = "VARCHAR2(36)" },
            new() { Id = "DT-008", Category = "数据类型", Description = "IMAGE -> BLOB", SqlServerPattern = "IMAGE", DM8Replacement = "BLOB" },
            new() { Id = "DT-009", Category = "数据类型", Description = "VARBINARY -> RAW", SqlServerPattern = "VARBINARY", DM8Replacement = "RAW" },
            new() { Id = "DT-010", Category = "数据类型", Description = "NTEXT -> TEXT", SqlServerPattern = "NTEXT", DM8Replacement = "TEXT" },
            new() { Id = "DT-011", Category = "数据类型", Description = "REAL -> FLOAT", SqlServerPattern = "REAL", DM8Replacement = "FLOAT" },
            new() { Id = "DT-012", Category = "数据类型", Description = "FLOAT -> DOUBLE", SqlServerPattern = "FLOAT", DM8Replacement = "DOUBLE" },

            // 内置函数映射
            new() { Id = "FN-001", Category = "内置函数", Description = "GETDATE() -> SYSDATE", SqlServerPattern = "GETDATE()", DM8Replacement = "SYSDATE" },
            new() { Id = "FN-002", Category = "内置函数", Description = "ISNULL -> NVL", SqlServerPattern = "ISNULL(a, b)", DM8Replacement = "NVL(a, b)" },
            new() { Id = "FN-003", Category = "内置函数", Description = "LEN -> LENGTH", SqlServerPattern = "LEN(x)", DM8Replacement = "LENGTH(x)" },
            new() { Id = "FN-004", Category = "内置函数", Description = "CHARINDEX -> INSTR (参数互换)", SqlServerPattern = "CHARINDEX(a, b)", DM8Replacement = "INSTR(b, a)" },
            new() { Id = "FN-005", Category = "内置函数", Description = "LEFT -> SUBSTR", SqlServerPattern = "LEFT(s, n)", DM8Replacement = "SUBSTR(s, 1, n)" },
            new() { Id = "FN-006", Category = "内置函数", Description = "RIGHT -> SUBSTR", SqlServerPattern = "RIGHT(s, n)", DM8Replacement = "SUBSTR(s, -n)" },
            new() { Id = "FN-007", Category = "内置函数", Description = "SUBSTRING -> SUBSTR", SqlServerPattern = "SUBSTRING", DM8Replacement = "SUBSTR" },
            new() { Id = "FN-008", Category = "内置函数", Description = "CONVERT -> CAST", SqlServerPattern = "CONVERT(type, expr)", DM8Replacement = "CAST(expr AS type)" },
            new() { Id = "FN-009", Category = "内置函数", Description = "NEWID -> SYS_GUID", SqlServerPattern = "NEWID()", DM8Replacement = "SYS_GUID()" },
            new() { Id = "FN-010", Category = "内置函数", Description = "STRING_AGG -> LISTAGG", SqlServerPattern = "STRING_AGG", DM8Replacement = "LISTAGG" },
            new() { Id = "FN-011", Category = "内置函数", Description = "IIF -> CASE WHEN", SqlServerPattern = "IIF(cond, t, f)", DM8Replacement = "CASE WHEN" },

            // 日期函数
            new() { Id = "DT-FN-001", Category = "日期函数", Description = "DATEADD(YEAR) -> ADD_MONTHS", SqlServerPattern = "DATEADD(YEAR, n, d)", DM8Replacement = "ADD_MONTHS(d, n*12)" },
            new() { Id = "DT-FN-002", Category = "日期函数", Description = "DATEADD(MONTH) -> ADD_MONTHS", SqlServerPattern = "DATEADD(MONTH, n, d)", DM8Replacement = "ADD_MONTHS(d, n)" },
            new() { Id = "DT-FN-003", Category = "日期函数", Description = "DATEADD(DAY) -> d + n", SqlServerPattern = "DATEADD(DAY, n, d)", DM8Replacement = "d + n" },
            new() { Id = "DT-FN-004", Category = "日期函数", Description = "DATEDIFF(YEAR) -> EXTRACT", SqlServerPattern = "DATEDIFF(YEAR, a, b)", DM8Replacement = "EXTRACT(YEAR FROM b) - EXTRACT(YEAR FROM a)" },
            new() { Id = "DT-FN-005", Category = "日期函数", Description = "DATEDIFF(MONTH) -> MONTHS_BETWEEN", SqlServerPattern = "DATEDIFF(MONTH, a, b)", DM8Replacement = "MONTHS_BETWEEN(b, a)" },
            new() { Id = "DT-FN-006", Category = "日期函数", Description = "DATEDIFF(DAY) -> TRUNC", SqlServerPattern = "DATEDIFF(DAY, a, b)", DM8Replacement = "TRUNC(b) - TRUNC(a)" },
            new() { Id = "DT-FN-007", Category = "日期函数", Description = "DATEPART -> EXTRACT", SqlServerPattern = "DATEPART(part, d)", DM8Replacement = "EXTRACT(part FROM d)" },
            new() { Id = "DT-FN-008", Category = "日期函数", Description = "YEAR/MONTH/DAY -> EXTRACT", SqlServerPattern = "YEAR(d)", DM8Replacement = "EXTRACT(YEAR FROM d)" },

            // 系统函数
            new() { Id = "SYS-001", Category = "系统函数", Description = "DB_NAME() -> SYS_CONTEXT", SqlServerPattern = "DB_NAME()", DM8Replacement = "SYS_CONTEXT('USERENV','DB_NAME')" },
            new() { Id = "SYS-002", Category = "系统函数", Description = "@@IDENTITY -> IDENTITY()", SqlServerPattern = "@@IDENTITY", DM8Replacement = "IDENTITY()" },
            new() { Id = "SYS-003", Category = "系统函数", Description = "@@ERROR -> SQLCODE", SqlServerPattern = "@@ERROR", DM8Replacement = "SQLCODE" },
            new() { Id = "SYS-004", Category = "系统函数", Description = "@@ROWCOUNT -> SQL%ROWCOUNT", SqlServerPattern = "@@ROWCOUNT", DM8Replacement = "SQL%ROWCOUNT" },

            // 语法结构
            new() { Id = "SY-001", Category = "语法结构", Description = "TOP N -> FETCH FIRST N ROWS ONLY", SqlServerPattern = "SELECT TOP N", DM8Replacement = "FETCH FIRST N ROWS ONLY" },
            new() { Id = "SY-002", Category = "语法结构", Description = "方括号 -> 双引号", SqlServerPattern = "[name]", DM8Replacement = "\"name\"" },
            new() { Id = "SY-003", Category = "语法结构", Description = "OUTER APPLY -> LEFT JOIN LATERAL", SqlServerPattern = "OUTER APPLY", DM8Replacement = "LEFT JOIN LATERAL" },
            new() { Id = "SY-004", Category = "语法结构", Description = "CROSS APPLY -> CROSS JOIN LATERAL", SqlServerPattern = "CROSS APPLY", DM8Replacement = "CROSS JOIN LATERAL" },
            new() { Id = "SY-005", Category = "语法结构", Description = "NOLOCK 提示移除", SqlServerPattern = "WITH (NOLOCK)", DM8Replacement = "(已移除)" },
            new() { Id = "SY-006", Category = "语法结构", Description = "CREATE -> CREATE OR REPLACE", SqlServerPattern = "CREATE VIEW/PROC", DM8Replacement = "CREATE OR REPLACE" },
            new() { Id = "SY-007", Category = "语法结构", Description = "TRY...CATCH -> EXCEPTION WHEN OTHERS", SqlServerPattern = "BEGIN TRY...CATCH", DM8Replacement = "EXCEPTION WHEN OTHERS THEN" },
            new() { Id = "SY-008", Category = "语法结构", Description = "ELSE IF -> ELSIF", SqlServerPattern = "ELSE IF", DM8Replacement = "ELSIF" },
            new() { Id = "SY-009", Category = "语法结构", Description = "WHILE...BEGIN...END -> LOOP...END LOOP", SqlServerPattern = "WHILE...BEGIN...END", DM8Replacement = "LOOP...END LOOP" },
            new() { Id = "SY-010", Category = "语法结构", Description = "SET @var = -> var :=", SqlServerPattern = "SET @var = value", DM8Replacement = "var := value" },
            new() { Id = "SY-011", Category = "语法结构", Description = "DECLARE @var -> var", SqlServerPattern = "DECLARE @var TYPE", DM8Replacement = "var TYPE" },
            new() { Id = "SY-012", Category = "语法结构", Description = "PRINT -> DBMS_OUTPUT.PUT_LINE", SqlServerPattern = "PRINT msg", DM8Replacement = "DBMS_OUTPUT.PUT_LINE(msg)" },
            new() { Id = "SY-013", Category = "语法结构", Description = "RAISERROR -> RAISE_APPLICATION_ERROR", SqlServerPattern = "RAISERROR", DM8Replacement = "RAISE_APPLICATION_ERROR" },
            new() { Id = "SY-014", Category = "语法结构", Description = "EXEC(@sql) -> EXECUTE IMMEDIATE", SqlServerPattern = "EXEC(@sql)", DM8Replacement = "EXECUTE IMMEDIATE sql" },
            new() { Id = "SY-015", Category = "语法结构", Description = "游标 -> FOR LOOP", SqlServerPattern = "DECLARE CURSOR FOR", DM8Replacement = "FOR rec IN (SELECT) LOOP" },
            new() { Id = "SY-016", Category = "语法结构", Description = "#临时表 -> 全局临时表", SqlServerPattern = "#temp", DM8Replacement = "TEMP_temp" },
            new() { Id = "SY-017", Category = "语法结构", Description = "BEGIN TRANSACTION -> BEGIN", SqlServerPattern = "BEGIN TRANSACTION", DM8Replacement = "BEGIN" },
            new() { Id = "SY-018", Category = "语法结构", Description = "SAVE TRANSACTION -> SAVEPOINT", SqlServerPattern = "SAVE TRANSACTION name", DM8Replacement = "SAVEPOINT name" },
        };
    }
}
