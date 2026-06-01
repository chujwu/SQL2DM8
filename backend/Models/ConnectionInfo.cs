namespace SQLServerToDM8.Models;

public class SqlServerConnectionInfo
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public bool UseWindowsAuth { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Database { get; set; }
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public double ElapsedMs { get; set; }
}

public class DatabaseInfo
{
    public string Name { get; set; } = string.Empty;
    public int Id { get; set; }
    public string? State { get; set; }
}
