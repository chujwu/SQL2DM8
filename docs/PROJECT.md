# SQL2DM8

> SQL Server → 达梦8 数据库对象转换工具

## 1. 项目概述

### 1.1 背景
在国产化替代进程中， SQL Server 数据库迁移至达梦8（DM8）是常见需求。本工具旨在提供一个可视化界面，帮助开发者高效地完成视图、函数、存储过程等 SQL 对象的语法转换工作。

### 1.2 核心功能
- **在线连接**：直接连接 SQL Server 数据库，读取数据库对象
- **对象浏览**：以树形结构展示视图、函数、存储过程（正确分类）
- **智能转换**：53 条转换规则，自动将 SQL Server 语法转换为 DM8 兼容语法
- **对照预览**：左右分栏展示原始 SQL 与转换后 SQL，差异高亮
- **手动编辑**：支持在转换结果上手动调整修正
- **批量导出**：一键导出所有转换后的 SQL 脚本文件（ZIP 格式）
- **置信度评估**：自动评估转换质量，标记需要人工处理的部分
- **连接配置管理**：保存和管理数据库连接配置
- **转换规则查看**：查看所有 53 条转换规则，支持分类筛选
- **功能演示**：内置 9 个示例对象，快速体验转换功能

### 1.3 目标用户
需要进行 SQL Server → DM8 迁移的 DBA 和后端开发工程师。

### 1.4 项目统计

| 指标 | 数值 |
|------|------|
| 后端代码 | 3,065 行 |
| 前端代码 | 2,481 行 |
| 总代码 | 5,546 行 |
| 转换规则 | 53 条 |
| 测试用例 | 38 个 |
| 测试通过率 | 100% |

---

## 2. 技术架构

### 2.1 技术栈

| 层级 | 技术选型 | 说明 |
|------|----------|------|
| 前端 | React 18 + TypeScript + Vite 5 | SPA 应用，Monaco Editor 提供 SQL 编辑体验 |
| 后端 | ASP.NET Core 8 Web API | 提供 RESTful API，处理数据库连接和 SQL 转换 |
| 数据库驱动 | Microsoft.Data.SqlClient | 连接 SQL Server |
| UI 组件库 | Ant Design 5 | 成熟的企业级 UI 组件库 |
| Diff 展示 | Monaco Diff Editor | VSCode 同款 diff 引擎 |
| HTTP 客户端 | Axios | 前端 API 调用 |
| 文件导出 | JSZip + FileSaver | ZIP 打包和文件下载 |

### 2.2 项目结构

```
SQLServerToDM8/
├── backend/                          # ASP.NET Core 8 Web API
│   ├── Controllers/
│   │   ├── ConnectionController.cs   # 数据库连接管理
│   │   ├── ObjectController.cs       # 数据库对象读取
│   │   ├── ConvertController.cs      # SQL 转换
│   │   ├── SampleController.cs       # 示例数据
│   │   └── TestController.cs         # 转换测试接口
│   ├── Services/
│   │   ├── ConversionEngine.cs       # SQL 转换引擎（核心，873行）
│   │   ├── DatabaseService.cs        # 数据库连接服务
│   │   ├── ExportService.cs          # 文件导出服务
│   │   └── SampleDataService.cs      # 示例数据服务
│   ├── Models/
│   │   ├── ConnectionInfo.cs         # 连接信息模型
│   │   ├── DatabaseObject.cs         # 数据库对象模型
│   │   └── ConvertResult.cs          # 转换结果模型
│   ├── Program.cs
│   └── SQLServerToDM8.csproj
├── frontend/                         # React 前端
│   ├── src/
│   │   ├── pages/
│   │   │   ├── ConnectionPage.tsx    # 连接配置页
│   │   │   ├── ObjectBrowser.tsx     # 对象浏览页
│   │   │   ├── ConvertPage.tsx       # 转换对比页
│   │   │   ├── RulesPage.tsx         # 转换规则页
│   │   │   └── SampleDemo.tsx        # 功能演示页
│   │   ├── components/
│   │   │   ├── ConnectionForm.tsx    # 连接表单
│   │   │   ├── ConnectionStatus.tsx  # 连接状态指示器
│   │   │   ├── ObjectTree.tsx        # 对象树组件
│   │   │   ├── SqlDiffViewer.tsx     # SQL Diff 查看器
│   │   │   └── ConversionPanel.tsx   # 转换面板
│   │   ├── services/
│   │   │   ├── api.ts                # API 调用封装
│   │   │   └── storage.ts            # 本地存储服务
│   │   └── types/
│   │       └── index.ts              # TypeScript 类型定义
│   ├── package.json
│   └── vite.config.ts
├── docs/
│   ├── PROJECT.md                    # 项目详细文档
│   └── REVIEW_REPORT.md              # 项目审查报告
├── start-backend.bat                 # 后端启动脚本
├── start-frontend.bat                # 前端启动脚本
├── restart-all.bat                   # 重启所有服务脚本
└── README.md
```

### 2.3 架构图

```
┌─────────────────────────────────────────────────────┐
│                   React 前端 (Vite)                  │
│  ┌──────────┐  ┌───────────┐  ┌──────────────────┐  │
│  │ 连接配置  │  │ 对象浏览树 │  │ Diff编辑器(左右) │  │
│  └─────┬────┘  └─────┬─────┘  └────────┬─────────┘  │
│        │             │                  │            │
│        └─────────────┼──────────────────┘            │
│                      │ HTTP/REST                     │
└──────────────────────┼───────────────────────────────┘
                       │
┌──────────────────────┼───────────────────────────────┐
│              ASP.NET Core 8 Web API                  │
│  ┌──────────┐  ┌─────┴──────┐  ┌──────────────────┐ │
│  │Connection│  │ Conversion │  │   Export Service  │ │
│  │Controller│  │  Engine    │  │                   │ │
│  └─────┬────┘  └────────────┘  └──────────────────┘ │
│        │                                              │
│  ┌─────┴──────────┐                                  │
│  │ Database Service│                                  │
│  └─────┬──────────┘                                  │
└────────┼──────────────────────────────────────────────┘
         │
    ┌────┴────┐
    │SQL Server│
    └─────────┘
```

---

## 3. API 设计

### 3.1 连接管理

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/connection/test | 测试数据库连接 |
| POST | /api/connection/databases | 获取数据库列表 |

### 3.2 数据库对象

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/objects/{database}/tree | 获取对象树 |
| POST | /api/objects/{database}/views | 获取视图列表 |
| POST | /api/objects/{database}/functions | 获取函数列表 |
| POST | /api/objects/{database}/procedures | 获取存储过程列表 |
| POST | /api/objects/{database}/{type}/{schema}/{name}/sql | 获取单个对象的 SQL 定义 |
| POST | /api/objects/{database}/test | 测试对象连接 |
| POST | /api/objects/{database}/debug | 调试：对象类型统计 |

### 3.3 SQL 转换

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/convert/single | 转换单个 SQL |
| POST | /api/convert/batch | 批量转换 |
| GET | /api/convert/rules | 获取转换规则列表 |
| POST | /api/convert/export | 下载转换结果（ZIP包） |

### 3.4 示例数据

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/sample/objects | 获取示例对象列表 |
| POST | /api/sample/convert | 转换所有示例 |
| POST | /api/sample/convert/{index} | 转换单个示例 |

### 3.5 测试接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/test/conversion | 运行转换测试用例（38个） |
| POST | /api/test/convert | 测试单个 SQL 转换 |

### 3.6 健康检查

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/health | 服务健康检查 |

---

## 4. SQL Server → DM8 转换规则

### 4.1 数据类型映射

| SQL Server 类型 | DM8 类型 | 备注 |
|-----------------|----------|------|
| NVARCHAR(n) | NVARCHAR2(n) | 统一改为 NVARCHAR2 |
| VARCHAR(n) | VARCHAR2(n) | 统一改为 VARCHAR2 |
| DATETIME | TIMESTAMP | |
| DATETIME2 | TIMESTAMP | |
| SMALLDATETIME | TIMESTAMP | |
| DATE | DATE | 无需转换 |
| TIME | TIME | 无需转换 |
| MONEY | DECIMAL(19,4) | |
| SMALLMONEY | DECIMAL(10,4) | |
| BIT | TINYINT | DM8 用 0/1 表示 |
| UNIQUEIDENTIFIER | VARCHAR2(36) | 存储 GUID 字符串 |
| IMAGE | BLOB | |
| VARBINARY(n) | RAW(n) | |
| NTEXT | TEXT | DM8 无 NTEXT |
| TEXT | TEXT | |
| REAL | FLOAT | |
| FLOAT | DOUBLE | DM8 的 FLOAT 是单精度 |
| INT | INT | 无需转换 |
| BIGINT | BIGINT | 无需转换 |
| SMALLINT | SMALLINT | 无需转换 |
| TINYINT | TINYINT | 无需转换 |
| DECIMAL | DECIMAL | 无需转换 |
| NUMERIC | NUMERIC | 无需转换 |

### 4.2 内置函数映射

| SQL Server 函数 | DM8 函数 | 备注 |
|-----------------|----------|------|
| GETDATE() | SYSDATE | |
| GETUTCDATE() | SYS_EXTRACT_UTC(SYSDATE) | |
| ISNULL(a, b) | NVL(a, b) | |
| LEN(x) | LENGTH(x) | |
| CHARINDEX(a, b) | INSTR(b, a) | 参数顺序互换 |
| CHARINDEX(a, b, n) | INSTR(b, a, n) | 参数顺序互换 |
| LEFT(s, n) | SUBSTR(s, 1, n) | |
| RIGHT(s, n) | SUBSTR(s, -n) | 或 SUBSTR(s, LENGTH(s)-n+1) |
| REPLACE(s, old, new) | REPLACE(s, old, new) | 无需转换 |
| UPPER(s) | UPPER(s) | 无需转换 |
| LOWER(s) | LOWER(s) | 无需转换 |
| LTRIM(s) | LTRIM(s) | 无需转换 |
| RTRIM(s) | RTRIM(s) | 无需转换 |
| SUBSTRING(s, n, l) | SUBSTR(s, n, l) | 函数名不同 |
| DATEADD(part, n, d) | 见下方规则 | 需要展开 |
| DATEDIFF(part, a, b) | 见下方规则 | 需要展开 |
| DATEPART(part, d) | 见下方规则 | 需要展开 |
| YEAR(d) | EXTRACT(YEAR FROM d) | |
| MONTH(d) | EXTRACT(MONTH FROM d) | |
| DAY(d) | EXTRACT(DAY FROM d) | |
| CONVERT(type, expr) | CAST(expr AS type) | |
| CONVERT(type, expr, style) | TO_CHAR/TO_DATE | 支持30+种样式 |
| NEWID() | SYS_GUID() | 返回格式不同需注意 |
| SCOPE_IDENTITY() | IDENTITY() 或 序列.CURRVAL | |
| @@IDENTITY | IDENTITY() | |
| @@ROWCOUNT | SQL%ROWCOUNT | |
| @@ERROR | SQLCODE | |
| DB_NAME() | SYS_CONTEXT('USERENV','DB_NAME') | |
| HOST_NAME() | SYS_CONTEXT('USERENV','HOST') | |
| SUSER_NAME() | SYS_CONTEXT('USERENV','SESSION_USER') | |
| CAST(x AS type) | CAST(x AS type) | 无需转换 |
| IIF(cond, t, f) | CASE WHEN cond THEN t ELSE f END | |
| STRING_AGG(col, sep) | LISTAGG(col, sep) | |

### 4.3 DATEADD 展开规则

| 日期部分 | DM8 表达式 |
|----------|-----------|
| DATEADD(YEAR, n, d) | ADD_MONTHS(d, n*12) |
| DATEADD(MONTH, n, d) | ADD_MONTHS(d, n) |
| DATEADD(DAY, n, d) | d + n |
| DATEADD(HOUR, n, d) | d + n/24 |
| DATEADD(MINUTE, n, d) | d + n/1440 |
| DATEADD(SECOND, n, d) | d + n/86400 |
| DATEADD(QUARTER, n, d) | ADD_MONTHS(d, n*3) |
| DATEADD(WEEK, n, d) | d + n*7 |

### 4.4 DATEDIFF 展开规则

| 日期部分 | DM8 表达式 |
|----------|-----------|
| DATEDIFF(YEAR, a, b) | EXTRACT(YEAR FROM b) - EXTRACT(YEAR FROM a) |
| DATEDIFF(MONTH, a, b) | MONTHS_BETWEEN(b, a) |
| DATEDIFF(DAY, a, b) | TRUNC(b) - TRUNC(a) |
| DATEDIFF(HOUR, a, b) | (b - a) * 24 |
| DATEDIFF(MINUTE, a, b) | (b - a) * 1440 |
| DATEDIFF(SECOND, a, b) | (b - a) * 86400 |

### 4.5 CONVERT 样式转换

| 样式号 | SQL Server 格式 | DM8 格式 |
|--------|-----------------|----------|
| 0 | Mon DD YYYY HH:MIAM/PM | YYYY-MM-DD HH24:MI:SS |
| 1 | MM/DD/YY | MM/DD/YY |
| 2 | YY.MM.DD | YY.MM.DD |
| 3 | DD/MM/YY | DD/MM/YY |
| 4 | DD.MM.YY | DD.MM.YY |
| 5 | DD-MM-YY | DD-MM-YY |
| 101 | MM/DD/YYYY | MM/DD/YYYY |
| 102 | YYYY.MM.DD | YYYY.MM.DD |
| 103 | DD/MM/YYYY | DD/MM/YYYY |
| 104 | DD.MM.YYYY | DD.MM.YYYY |
| 105 | DD-MM-YYYY | DD-MM-YYYY |
| 110 | MM-DD-YYYY | MM-DD-YYYY |
| 111 | YYYY/MM/DD | YYYY/MM/DD |
| 112 | YYYYMMDD | YYYYMMDD |
| 120 | YYYY-MM-DD HH:MI:SS | YYYY-MM-DD HH24:MI:SS |
| 121 | YYYY-MM-DD HH:MI:SS | YYYY-MM-DD HH24:MI:SS |
| 126 | YYYY-MM-DDTHH:MI:SS | YYYY-MM-DDTHH24:MI:SS |

### 4.6 语法结构转换

| SQL Server 语法 | DM8 语法 | 说明 |
|-----------------|----------|------|
| SELECT TOP N ... | SELECT ... FETCH FIRST N ROWS ONLY | 或用 ROWNUM |
| SELECT TOP N PERCENT ... | 需用 ROWNUM 计算 | |
| SELECT TOP (N) WITH TIES ... | 需用窗口函数 | |
| NOLOCK 提示 | 删除 | DM8 不支持此提示 |
| [表名] / [列名] | "表名" / "列名" | 方括号改双引号 |
| += 字符串拼接 | \|\| | |
| STRING_AGG(col, sep) | LISTAGG(col, sep) | |
| PIVOT | DM8 支持 PIVOT | 语法基本相同 |
| UNPIVOT | DM8 支持 UNPIVOT | 语法基本相同 |
| OUTER APPLY | LEFT JOIN LATERAL | DM8 12c+ 支持 |
| CROSS APPLY | CROSS JOIN LATERAL | |
| IF ... BEGIN ... END | IF ... THEN ... END IF; | 存储过程/函数内部 |
| ELSE IF | ELSIF | |
| WHILE ... BEGIN ... END | LOOP ... END LOOP; | 需配合 EXIT WHEN |
| BEGIN TRY ... END TRY | BEGIN ... EXCEPTION WHEN OTHERS THEN | |
| BEGIN CATCH ... END CATCH | EXCEPTION WHEN OTHERS THEN ... | |
| PRINT msg | DBMS_OUTPUT.PUT_LINE(msg); | |
| EXEC sp_name | sp_name; | 直接调用 |
| EXEC(sql) | EXECUTE IMMEDIATE sql; | |
| SET NOCOUNT ON | 删除 | DM8 无需此设置 |
| SET @var = value | var := value; | 赋值语法 |
| DECLARE @var TYPE | var TYPE; | 声明语法 |
| RAISERROR(msg, ...) | RAISE_APPLICATION_ERROR(code, msg); | |
| RETURN value | RETURN value; | 函数中相同 |
| #临时表 | 全局临时表 TEMP_ | DM8 支持临时表 |
| DECLARE @table TABLE (...) | 全局临时表 | 表变量转换提示 |
| DECLARE cursor CURSOR FOR | FOR rec IN (SELECT) LOOP | 游标转换提示 |
| BEGIN TRANSACTION | BEGIN | |
| SAVE TRANSACTION name | SAVEPOINT name | |
| COMMIT TRANSACTION | COMMIT | |
| ROLLBACK TRANSACTION | ROLLBACK | |

### 4.7 CREATE 语句模板转换

#### 视图

```sql
-- SQL Server
CREATE VIEW [dbo].[vw_xxx]
AS
SELECT ...

-- DM8
CREATE OR REPLACE VIEW "dbo"."vw_xxx"
AS
SELECT ...;
```

#### 存储过程

```sql
-- SQL Server
CREATE PROCEDURE [dbo].[sp_xxx]
    @param1 INT,
    @param2 NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    ...
END

-- DM8
CREATE OR REPLACE PROCEDURE "dbo"."sp_xxx"
(
    param1 IN INT,
    param2 IN NVARCHAR2(50)
)
AS
BEGIN
    ...;
END;
```

#### 函数

```sql
-- SQL Server
CREATE FUNCTION [dbo].[fn_xxx](@param1 INT)
RETURNS NVARCHAR(100)
AS
BEGIN
    DECLARE @result NVARCHAR(100)
    ...
    RETURN @result
END

-- DM8
CREATE OR REPLACE FUNCTION "dbo"."fn_xxx"(param1 IN INT)
RETURN NVARCHAR2(100)
AS
    result NVARCHAR2(100);
BEGIN
    ...;
    RETURN result;
END;
```

---

## 5. UI 设计

### 5.1 页面流程

```
连接配置页 --> 对象浏览页 --> 转换对比页
   (1)           (2)            (3)
```

### 5.2 页面列表

| 页面 | 路径 | 功能 |
|------|------|------|
| 连接配置页 | / | 数据库连接配置、测试、保存 |
| 对象浏览页 | /browse | 对象树浏览、SQL 预览 |
| 转换对比页 | /convert | 批量转换、Diff 对比、手动编辑 |
| 转换规则页 | /rules | 查看所有转换规则 |
| 功能演示页 | /demo | 示例数据演示 |

### 5.3 连接配置页
- 服务器地址、端口
- 认证方式（Windows认证 / SQL Server认证）
- 用户名、密码
- 数据库选择（下拉列表，连接后动态加载）
- 测试连接 / 连接按钮
- 保存连接配置
- 最近连接列表

### 5.4 对象浏览页
- 左侧：对象树（按 视图/函数/存储过程 分组，显示 schema 和对象数量）
- 右侧：SQL 预览区（选中对象后显示原始 SQL）
- 顶部：统计信息（视图、函数、存储过程数量）、搜索过滤
- 底部：「开始转换」按钮

### 5.5 转换对比页
- 左侧：原始 SQL（只读高亮）
- 右侧：转换后 SQL（可编辑，Monaco Editor）
- 差异行高亮标记（新增=绿、删除=红、修改=黄）
- 转换警告/提示面板（显示无法自动转换的部分）
- 顶部操作栏：上一个/下一个对象、重置编辑、导出当前、导出全部
- 统计面板：总对象数、成功数、警告数、错误数

### 5.6 转换规则页
- 规则列表（支持分类筛选和搜索）
- 规则概览（按类别统计）
- 数据类型映射表
- 内置函数映射表
- 语法结构转换表

### 5.7 功能演示页
- 示例对象列表（9个示例）
- 一键转换所有示例
- 单个示例转换
- 转换统计信息

### 5.8 导出
- 导出为 ZIP 包，按对象类型分目录
- 文件命名：{schema}.{objectName}.sql
- 目录结构：

```
export/
├── Views/
│   ├── dbo.vw_xxx.sql
│   └── dbo.vw_yyy.sql
├── Functions/
│   ├── dbo.fn_xxx.sql
│   └── dbo.fn_yyy.sql
└── Procedures/
    ├── dbo.sp_xxx.sql
    └── dbo.sp_yyy.sql
```

---

## 6. 转换引擎设计

### 6.1 转换流水线

```
原始SQL --> 字符串保护 --> 预处理 --> 数据类型转换 --> 函数转换 --> 语法转换 --> 后处理 --> 结果SQL + 警告列表
```

### 6.2 转换引擎架构

```csharp
public class ConversionEngine : IConversionEngine
{
    // 转换入口
    public ConvertResult Convert(string sql, DatabaseObjectType objectType, string objectName, string schema)
    
    // 预处理
    private string PreProcess(string sql)
    
    // 数据类型转换
    private string ConvertDataTypes(string sql)
    
    // 内置函数转换
    private string ConvertBuiltInFunctions(string sql)
    
    // 日期函数转换
    private string ConvertDateFunctions(string sql)
    
    // 语法结构转换
    private string ConvertSyntax(string sql, DatabaseObjectType objectType)
    
    // 后处理
    private string PostProcess(string sql)
}
```

### 6.3 转换结果数据结构

```json
{
  "objectName": "sp_GetData",
  "schema": "dbo",
  "objectType": "Procedure",
  "originalSql": "...",
  "convertedSql": "...",
  "warnings": [
    {
      "line": 15,
      "column": 10,
      "message": "游标需要转换为 FOR LOOP",
      "severity": "Warning"
    }
  ],
  "confidence": 0.85,
  "convertible": true
}
```

### 6.4 置信度评估
- 所有规则命中且无手动处理项：confidence = 1.0
- 存在警告：每条警告 -0.1
- 存在无法转换的语法：confidence = 0.3
- 转换引擎抛出异常：confidence = 0，标记 convertible = false

---

## 7. 测试

### 7.1 测试用例（38个）

| 类别 | 用例数 | 通过率 |
|------|--------|--------|
| 数据类型转换 | 5 | 100% |
| 内置函数转换 | 6 | 100% |
| 日期函数转换 | 5 | 100% |
| 语法结构转换 | 7 | 100% |
| 存储过程语法 | 6 | 100% |
| 系统函数转换 | 3 | 100% |
| 其他转换 | 6 | 100% |
| **总计** | **38** | **100%** |

### 7.2 运行测试

```bash
# 启动后端后访问
curl http://localhost:5000/api/test/conversion
```

### 7.3 测试覆盖详情

#### 数据类型转换
- NVARCHAR → NVARCHAR2 ✅
- VARCHAR → VARCHAR2 ✅
- DATETIME → TIMESTAMP ✅
- MONEY → DECIMAL(19,4) ✅
- BIT → TINYINT ✅

#### 内置函数转换
- GETDATE() → SYSDATE ✅
- ISNULL → NVL ✅
- LEN → LENGTH ✅
- CHARINDEX → INSTR ✅
- LEFT → SUBSTR ✅
- RIGHT → SUBSTR ✅

#### 日期函数转换
- DATEADD YEAR → ADD_MONTHS ✅
- DATEADD DAY → d + n ✅
- DATEDIFF DAY → TRUNC ✅
- DATEDIFF MONTH → MONTHS_BETWEEN ✅
- YEAR → EXTRACT ✅

#### 语法结构转换
- TOP N → FETCH FIRST ✅
- 方括号 → 双引号 ✅
- OUTER APPLY → LEFT JOIN LATERAL ✅
- CROSS APPLY → CROSS JOIN LATERAL ✅
- NOLOCK 移除 ✅
- SET NOCOUNT 移除 ✅
- CREATE → CREATE OR REPLACE ✅

#### 存储过程语法
- SET @var → var := ✅
- DECLARE @var → var ✅
- TRY...CATCH → EXCEPTION ✅
- RAISERROR → RAISE_APPLICATION_ERROR ✅
- EXEC(@sql) → EXECUTE IMMEDIATE ✅
- 综合存储过程 ✅

#### 系统函数转换
- @@IDENTITY → IDENTITY() ✅
- @@ERROR → SQLCODE ✅
- DB_NAME → SYS_CONTEXT ✅

#### 其他转换
- IIF → CASE WHEN ✅
- CONVERT 带样式 → TO_CHAR ✅
- NEWID → SYS_GUID ✅
- 临时表 → 全局临时表 ✅
- BEGIN TRANSACTION → BEGIN ✅
- SAVE TRANSACTION → SAVEPOINT ✅

---

## 8. 实施路线

### Phase 1：基础框架 ✅
- [x] ASP.NET Core Web API 项目搭建
- [x] 数据库连接测试接口
- [x] React + Vite 项目搭建
- [x] 基础布局和路由

### Phase 2：数据库对象读取 ✅
- [x] SQL Server 系统视图查询（sys.objects, sys.schemas）
- [x] 对象树 API（正确分类视图、函数、存储过程）
- [x] 前端对象树组件（显示统计信息）

### Phase 3：转换引擎 ✅
- [x] 数据类型映射（12条）
- [x] 内置函数映射（11条）
- [x] 日期函数展开（8条）
- [x] 语法结构转换（18条）
- [x] 系统函数映射（4条）
- [x] CONVERT 样式转换（30+种）
- [x] Diff 对比展示

### Phase 4：完整功能 ✅
- [x] 批量转换
- [x] 手动编辑
- [x] 文件导出（ZIP）
- [x] 转换警告提示
- [x] 连接配置保存
- [x] 转换规则查看
- [x] 功能演示
- [x] 转换测试（38个用例）

---

## 9. 依赖清单

### 后端 NuGet 包
- Microsoft.Data.SqlClient
- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore（Swagger）

### 前端 npm 包
- react, react-dom, react-router-dom
- antd（UI 组件库）
- @monaco-editor/react（SQL 编辑器 + Diff）
- axios（HTTP 客户端）
- jszip, file-saver（文件下载）
- @ant-design/icons（图标库）

---

## 10. 已知局限性

| 局限 | 影响 | 建议 |
|------|------|------|
| 字符串内关键字可能被误替换 | 低 | 已实现保护机制，可按需启用 |
| 复杂嵌套结构解析不完整 | 低 | 大部分场景可正确处理 |
| 游标需要手动调整 | 低 | 已添加转换提示 |
| 表变量需手动处理 | 低 | 已添加转换提示 |

---

## 11. 后续优化建议

### 高优先级
1. 字符串保护机制完善
2. 注释保护机制
3. 更多 CONVERT 样式支持

### 中优先级
4. 游标完整转换（自动生成 FOR LOOP 代码）
5. 表变量完整转换（自动生成 PL/SQL 集合类型）
6. 嵌套结构深度解析

### 低优先级
7. 单元测试覆盖率提升
8. 性能优化（大文件处理）
9. 转换历史记录功能
