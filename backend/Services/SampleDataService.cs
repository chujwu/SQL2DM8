using SQLServerToDM8.Models;

namespace SQLServerToDM8.Services;

public interface ISampleDataService
{
    List<SampleSqlObject> GetSampleObjects();
}

public class SampleSqlObject
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public DatabaseObjectType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SqlServerSql { get; set; } = string.Empty;
}

public class SampleDataService : ISampleDataService
{
    public List<SampleSqlObject> GetSampleObjects()
    {
        return new List<SampleSqlObject>
        {
            // 视图示例
            new()
            {
                Name = "vw_EmployeeInfo",
                Schema = "dbo",
                Type = DatabaseObjectType.View,
                Description = "员工信息视图 - 展示数据类型和函数转换",
                SqlServerSql = @"CREATE VIEW [dbo].[vw_EmployeeInfo]
AS
SELECT 
    [EmployeeID],
    [FirstName] + ' ' + [LastName] AS [FullName],
    [Email],
    [Phone],
    [HireDate],
    DATEDIFF(YEAR, [HireDate], GETDATE()) AS [YearsOfService],
    ISNULL([DepartmentName], 'Unknown') AS [Department],
    CASE WHEN [IsActive] = 1 THEN 'Active' ELSE 'Inactive' END AS [Status],
    CONVERT(NVARCHAR(50), [Salary], 1) AS [SalaryFormatted],
    [CreatedDate],
    [ModifiedDate]
FROM [dbo].[Employees] e WITH (NOLOCK)
LEFT JOIN [dbo].[Departments] d WITH (NOLOCK) ON e.[DepartmentID] = d.[DepartmentID]
WHERE [IsActive] = 1"
            },

            // 视图示例2 - 包含日期函数
            new()
            {
                Name = "vw_MonthlySales",
                Schema = "dbo",
                Type = DatabaseObjectType.View,
                Description = "月度销售视图 - 展示日期函数转换",
                SqlServerSql = @"CREATE VIEW [dbo].[vw_MonthlySales]
AS
SELECT 
    YEAR([OrderDate]) AS [OrderYear],
    MONTH([OrderDate]) AS [OrderMonth],
    COUNT(*) AS [OrderCount],
    SUM([TotalAmount]) AS [TotalSales],
    AVG([TotalAmount]) AS [AvgOrderAmount],
    DATEADD(MONTH, 1, DATEFROMPARTS(YEAR([OrderDate]), MONTH([OrderDate]), 1)) AS [NextMonthStart],
    DATEDIFF(DAY, MIN([OrderDate]), MAX([OrderDate])) AS [DaysInRange]
FROM [dbo].[Orders] WITH (NOLOCK)
WHERE [OrderDate] >= DATEADD(YEAR, -1, GETDATE())
GROUP BY YEAR([OrderDate]), MONTH([OrderDate])"
            },

            // 函数示例
            new()
            {
                Name = "fn_CalculateAge",
                Schema = "dbo",
                Type = DatabaseObjectType.Function,
                Description = "计算年龄函数 - 展示函数语法转换",
                SqlServerSql = @"CREATE FUNCTION [dbo].[fn_CalculateAge](@BirthDate DATE)
RETURNS INT
AS
BEGIN
    DECLARE @Age INT
    
    SET @Age = DATEDIFF(YEAR, @BirthDate, GETDATE())
    
    IF (MONTH(@BirthDate) > MONTH(GETDATE())) OR 
       (MONTH(@BirthDate) = MONTH(GETDATE()) AND DAY(@BirthDate) > DAY(GETDATE()))
    BEGIN
        SET @Age = @Age - 1
    END
    
    RETURN @Age
END"
            },

            // 函数示例2 - 字符串处理
            new()
            {
                Name = "fn_FormatPhone",
                Schema = "dbo",
                Type = DatabaseObjectType.Function,
                Description = "格式化电话号码函数 - 展示字符串函数转换",
                SqlServerSql = @"CREATE FUNCTION [dbo].[fn_FormatPhone](@Phone NVARCHAR(20))
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @CleanPhone NVARCHAR(20)
    DECLARE @FormattedPhone NVARCHAR(20)
    
    -- 移除非数字字符
    SET @CleanPhone = REPLACE(REPLACE(REPLACE(REPLACE(@Phone, '-', ''), ' ', ''), '(', ''), ')', '')
    
    -- 格式化为 (xxx) xxx-xxxx
    IF LEN(@CleanPhone) = 10
    BEGIN
        SET @FormattedPhone = '(' + LEFT(@CleanPhone, 3) + ') ' + 
                              SUBSTRING(@CleanPhone, 4, 3) + '-' + 
                              RIGHT(@CleanPhone, 4)
    END
    ELSE
    BEGIN
        SET @FormattedPhone = @Phone
    END
    
    RETURN @FormattedPhone
END"
            },

            // 存储过程示例
            new()
            {
                Name = "sp_GetEmployeeReport",
                Schema = "dbo",
                Type = DatabaseObjectType.Procedure,
                Description = "获取员工报表存储过程 - 展示存储过程语法转换",
                SqlServerSql = @"CREATE PROCEDURE [dbo].[sp_GetEmployeeReport]
    @DepartmentID INT = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SQL NVARCHAR(MAX)
    DECLARE @ParamList NVARCHAR(500)
    
    SET @SQL = N'SELECT 
        e.[EmployeeID],
        e.[FirstName] + '' '' + e.[LastName] AS [FullName],
        d.[DepartmentName],
        e.[HireDate],
        e.[Salary]
    FROM [dbo].[Employees] e WITH (NOLOCK)
    INNER JOIN [dbo].[Departments] d WITH (NOLOCK) ON e.[DepartmentID] = d.[DepartmentID]
    WHERE 1=1'
    
    IF @DepartmentID IS NOT NULL
    BEGIN
        SET @SQL = @SQL + N' AND e.[DepartmentID] = @DeptID'
    END
    
    IF @StartDate IS NOT NULL
    BEGIN
        SET @SQL = @SQL + N' AND e.[HireDate] >= @Start'
    END
    
    IF @EndDate IS NOT NULL
    BEGIN
        SET @SQL = @SQL + N' AND e.[HireDate] <= @End'
    END
    
    SET @SQL = @SQL + N' ORDER BY e.[LastName], e.[FirstName]'
    
    EXEC sp_executesql @SQL, 
        N'@DeptID INT, @Start DATE, @End DATE',
        @DeptID = @DepartmentID,
        @Start = @StartDate,
        @End = @EndDate
END"
            },

            // 存储过程示例2 - 包含错误处理
            new()
            {
                Name = "sp_UpdateEmployeeSalary",
                Schema = "dbo",
                Type = DatabaseObjectType.Procedure,
                Description = "更新员工薪资存储过程 - 展示TRY...CATCH转换",
                SqlServerSql = @"CREATE PROCEDURE [dbo].[sp_UpdateEmployeeSalary]
    @EmployeeID INT,
    @NewSalary DECIMAL(18,2),
    @EffectiveDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 验证员工存在
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [EmployeeID] = @EmployeeID)
        BEGIN
            RAISERROR('Employee not found', 16, 1);
            RETURN;
        END
        
        -- 验证薪资范围
        IF @NewSalary < 0 OR @NewSalary > 999999
        BEGIN
            RAISERROR('Invalid salary amount', 16, 1);
            RETURN;
        END
        
        -- 记录历史
        INSERT INTO [dbo].[SalaryHistory]
            ([EmployeeID], [OldSalary], [NewSalary], [EffectiveDate], [ModifiedDate])
        SELECT 
            @EmployeeID,
            [Salary],
            @NewSalary,
            @EffectiveDate,
            GETDATE()
        FROM [dbo].[Employees]
        WHERE [EmployeeID] = @EmployeeID;
        
        -- 更新薪资
        UPDATE [dbo].[Employees]
        SET [Salary] = @NewSalary,
            [ModifiedDate] = GETDATE()
        WHERE [EmployeeID] = @EmployeeID;
        
        COMMIT TRANSACTION;
        
        SELECT 'Success' AS [Status], @@ROWCOUNT AS [RowsAffected];
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END"
            },

            // 视图示例3 - 包含子查询和CASE
            new()
            {
                Name = "vw_ProductInventory",
                Schema = "dbo",
                Type = DatabaseObjectType.View,
                Description = "产品库存视图 - 展示复杂查询转换",
                SqlServerSql = @"CREATE VIEW [dbo].[vw_ProductInventory]
AS
SELECT 
    p.[ProductID],
    p.[ProductName],
    p.[ProductNumber],
    c.[CategoryName],
    ISNULL(p.[Color], 'N/A') AS [Color],
    p.[ListPrice],
    p.[StandardCost],
    ISNULL(i.[Quantity], 0) AS [QuantityInStock],
    CASE 
        WHEN ISNULL(i.[Quantity], 0) = 0 THEN 'Out of Stock'
        WHEN ISNULL(i.[Quantity], 0) < 10 THEN 'Low Stock'
        WHEN ISNULL(i.[Quantity], 0) < 50 THEN 'Medium Stock'
        ELSE 'In Stock'
    END AS [StockStatus],
    (SELECT MAX([OrderDate]) FROM [dbo].[PurchaseOrderDetails] pod 
     WHERE pod.[ProductID] = p.[ProductID]) AS [LastOrderDate],
    DATEDIFF(DAY, 
        (SELECT MAX([OrderDate]) FROM [dbo].[PurchaseOrderDetails] pod 
         WHERE pod.[ProductID] = p.[ProductID]),
        GETDATE()
    ) AS [DaysSinceLastOrder]
FROM [dbo].[Products] p WITH (NOLOCK)
LEFT JOIN [dbo].[Categories] c WITH (NOLOCK) ON p.[CategoryID] = c.[CategoryID]
LEFT JOIN [dbo].[Inventory] i WITH (NOLOCK) ON p.[ProductID] = i.[ProductID]"
            },

            // 函数示例3 - 表值函数
            new()
            {
                Name = "fn_SplitString",
                Schema = "dbo",
                Type = DatabaseObjectType.Function,
                Description = "字符串分割函数 - 展示表值函数转换",
                SqlServerSql = @"CREATE FUNCTION [dbo].[fn_SplitString]
(
    @InputString NVARCHAR(MAX),
    @Delimiter NVARCHAR(10)
)
RETURNS @OutputTable TABLE
(
    [ItemID] INT IDENTITY(1,1),
    [ItemValue] NVARCHAR(MAX)
)
AS
BEGIN
    DECLARE @StartPos INT = 1
    DECLARE @EndPos INT
    
    WHILE @StartPos <= LEN(@InputString)
    BEGIN
        SET @EndPos = CHARINDEX(@Delimiter, @InputString, @StartPos)
        
        IF @EndPos = 0
        BEGIN
            INSERT INTO @OutputTable ([ItemValue])
            VALUES (SUBSTRING(@InputString, @StartPos, LEN(@InputString) - @StartPos + 1))
            
            BREAK
        END
        
        INSERT INTO @OutputTable ([ItemValue])
        VALUES (SUBSTRING(@InputString, @StartPos, @EndPos - @StartPos))
        
        SET @StartPos = @EndPos + LEN(@Delimiter)
    END
    
    RETURN
END"
            },

            // 存储过程示例3 - 游标使用
            new()
            {
                Name = "sp_ProcessBatchOrders",
                Schema = "dbo",
                Type = DatabaseObjectType.Procedure,
                Description = "批量处理订单存储过程 - 展示游标和循环转换",
                SqlServerSql = @"CREATE PROCEDURE [dbo].[sp_ProcessBatchOrders]
    @BatchSize INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OrderID INT
    DECLARE @OrderCount INT = 0
    DECLARE @ProcessedCount INT = 0
    DECLARE @ErrorCount INT = 0
    
    -- 获取待处理订单
    DECLARE order_cursor CURSOR FOR
        SELECT TOP (@BatchSize) [OrderID]
        FROM [dbo].[Orders]
        WHERE [Status] = 'Pending'
        ORDER BY [OrderDate]
    
    OPEN order_cursor
    FETCH NEXT FROM order_cursor INTO @OrderID
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- 处理订单
            UPDATE [dbo].[Orders]
            SET [Status] = 'Processing',
                [ProcessedDate] = GETDATE()
            WHERE [OrderID] = @OrderID
            
            -- 更新库存
            UPDATE inv
            SET inv.[Quantity] = inv.[Quantity] - od.[Quantity]
            FROM [dbo].[Inventory] inv
            INNER JOIN [dbo].[OrderDetails] od ON inv.[ProductID] = od.[ProductID]
            WHERE od.[OrderID] = @OrderID
            
            SET @ProcessedCount = @ProcessedCount + 1
        END TRY
        BEGIN CATCH
            SET @ErrorCount = @ErrorCount + 1
            
            -- 记录错误
            INSERT INTO [dbo].[ErrorLog]
                ([ErrorMessage], [ErrorDate], [OrderID])
            VALUES
                (ERROR_MESSAGE(), GETDATE(), @OrderID)
        END CATCH
        
        FETCH NEXT FROM order_cursor INTO @OrderID
    END
    
    CLOSE order_cursor
    DEALLOCATE order_cursor
    
    -- 返回结果
    SELECT 
        @ProcessedCount AS [ProcessedOrders],
        @ErrorCount AS [FailedOrders],
        GETDATE() AS [CompletionTime]
END"
            }
        };
    }
}
