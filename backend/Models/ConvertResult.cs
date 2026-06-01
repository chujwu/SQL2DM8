namespace SQLServerToDM8.Models;

public class ConvertResult
{
    public string ObjectName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public DatabaseObjectType ObjectType { get; set; }
    public string OriginalSql { get; set; } = string.Empty;
    public string ConvertedSql { get; set; } = string.Empty;
    public List<ConvertWarning> Warnings { get; set; } = new();
    public double Confidence { get; set; } = 1.0;
    public bool Convertible { get; set; } = true;
}

public class ConvertWarning
{
    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = string.Empty;
    public WarningSeverity Severity { get; set; } = WarningSeverity.Warning;
}

public enum WarningSeverity
{
    Info,
    Warning,
    Error
}

public class BatchConvertRequest
{
    public string Database { get; set; } = string.Empty;
    public List<ObjectIdentifier> Objects { get; set; } = new();
}

public class ObjectIdentifier
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public DatabaseObjectType Type { get; set; }
}

public class BatchConvertResult
{
    public List<ConvertResult> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
}
