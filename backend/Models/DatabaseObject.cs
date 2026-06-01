namespace SQLServerToDM8.Models;

public enum DatabaseObjectType
{
    View,
    Function,
    Procedure
}

public class DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public DatabaseObjectType Type { get; set; }
    public string? Definition { get; set; }
    public DateTime? ModifyDate { get; set; }
}

public class ObjectTreeNode
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsLeaf { get; set; }
    public string? Icon { get; set; }
    public List<ObjectTreeNode> Children { get; set; } = new();
}

public class SqlDefinition
{
    public string ObjectName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public DatabaseObjectType ObjectType { get; set; }
    public string Sql { get; set; } = string.Empty;
}
