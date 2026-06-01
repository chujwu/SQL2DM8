# SQL2DM8

> SQL Server → 达梦8 数据库对象转换工具

一个用于将 SQL Server 数据库对象（视图、函数、存储过程）转换为达梦8 (DM8) 兼容 SQL 的可视化工具。

**测试通过率：100%** (38/38 测试用例)

---

## ✨ 功能特性

- 🔄 **智能转换**：53 条转换规则，自动将 SQL Server 语法转换为 DM8 兼容语法
- 📊 **差异对比**：Monaco Editor 左右分栏展示原始 SQL 与转换后 SQL
- ✏️ **手动编辑**：支持在转换结果上手动调整修正
- 📦 **批量导出**：一键导出所有转换后的 SQL 脚本文件（ZIP 格式）
- 🎯 **置信度评估**：自动评估转换质量，标记需要人工处理的部分
- 💾 **连接配置管理**：保存和管理数据库连接配置，支持最近连接
- 📚 **转换规则查看**：查看所有 53 条转换规则，支持分类筛选
- 🧪 **功能演示**：内置 9 个示例对象，快速体验转换功能
- 🔍 **对象分类浏览**：正确分类显示视图、函数、存储过程
- 📝 **CONVERT 样式支持**：支持 30+ 种日期格式样式转换

---

## 🛠️ 技术栈

### 后端
- ASP.NET Core 8 Web API
- Microsoft.Data.SqlClient（SQL Server 连接）
- Swagger/OpenAPI
- 日志记录（ILogger）

### 前端
- React 18 + TypeScript
- Vite 5
- Ant Design 5
- Monaco Editor（SQL 编辑器 + Diff 视图）
- Axios（HTTP 客户端）
- JSZip + FileSaver（文件导出）

---

## 🚀 快速开始

### 前置要求
- .NET 8 SDK
- Node.js 18+
- SQL Server 数据库访问权限（可选，有演示模式）

### 方式一：使用启动脚本（Windows）

```bash
# 双击运行
start-backend.bat    # 启动后端
start-frontend.bat   # 启动前端
```

### 方式二：手动启动

#### 启动后端

```bash
cd backend
dotnet restore
dotnet run
```

后端将在 http://localhost:5000 启动
Swagger UI: http://localhost:5000/swagger
健康检查: http://localhost:5000/api/health

#### 启动前端

```bash
cd frontend
npm install
npm run dev
```

前端将在 http://localhost:5173 启动

---

## 📖 使用说明

### 1. 功能演示（无需数据库）

1. 访问 http://localhost:5173
2. 点击顶部菜单"功能演示"
3. 点击"一键转换所有示例"
4. 查看转换结果和差异对比

### 2. 连接数据库转换

1. **配置连接**：填写 SQL Server 连接信息，测试连接并选择数据库
2. **浏览对象**：在对象树中查看视图、函数、存储过程（正确分类）
3. **选择对象**：勾选需要转换的对象
4. **开始转换**：点击"开始转换"按钮
5. **查看结果**：在对比视图中查看转换结果，检查警告
6. **手动调整**：如需要，可手动编辑转换后的 SQL
7. **导出文件**：点击"导出 ZIP"下载转换后的 SQL 文件

### 3. 保存连接配置

1. 填写连接信息后，点击保存按钮
2. 输入配置名称
3. 下次使用时，点击文件夹按钮加载已保存的配置

### 4. 查看转换规则

1. 点击顶部菜单"转换规则"
2. 查看所有 53 条转换规则
3. 支持按分类筛选和搜索

---

## 🔄 转换规则（53条）

### 数据类型映射（12条）

| SQL Server | DM8 | 说明 |
|------------|-----|------|
| NVARCHAR | NVARCHAR2 | 统一改为 NVARCHAR2 |
| VARCHAR | VARCHAR2 | 统一改为 VARCHAR2 |
| DATETIME | TIMESTAMP | |
| DATETIME2 | TIMESTAMP | |
| SMALLDATETIME | TIMESTAMP | |
| MONEY | DECIMAL(19,4) | |
| SMALLMONEY | DECIMAL(10,4) | |
| BIT | TINYINT | DM8 用 0/1 表示 |
| UNIQUEIDENTIFIER | VARCHAR2(36) | 存储 GUID 字符串 |
| IMAGE | BLOB | |
| VARBINARY | RAW | |
| NTEXT | TEXT | DM8 无 NTEXT |
| REAL | FLOAT | |
| FLOAT | DOUBLE | DM8 的 FLOAT 是单精度 |

### 内置函数映射（11条）

| SQL Server | DM8 | 说明 |
|------------|-----|------|
| GETDATE() | SYSDATE | |
| GETUTCDATE() | SYS_EXTRACT_UTC(SYSDATE) | |
| ISNULL(a, b) | NVL(a, b) | |
| LEN(x) | LENGTH(x) | |
| CHARINDEX(a, b) | INSTR(b, a) | 参数顺序互换 |
| LEFT(s, n) | SUBSTR(s, 1, n) | |
| RIGHT(s, n) | SUBSTR(s, -n) | |
| SUBSTRING(s, n, l) | SUBSTR(s, n, l) | 函数名不同 |
| CONVERT(type, expr) | CAST(expr AS type) | |
| NEWID() | SYS_GUID() | 返回格式不同 |
| STRING_AGG(col, sep) | LISTAGG(col, sep) | |
| IIF(cond, t, f) | CASE WHEN cond THEN t ELSE f END | |

### 日期函数映射（8条）

| SQL Server | DM8 | 说明 |
|------------|-----|------|
| DATEADD(YEAR, n, d) | ADD_MONTHS(d, n*12) | |
| DATEADD(MONTH, n, d) | ADD_MONTHS(d, n) | |
| DATEADD(DAY, n, d) | d + n | |
| DATEADD(HOUR, n, d) | d + n/24 | |
| DATEADD(MINUTE, n, d) | d + n/1440 | |
| DATEADD(SECOND, n, d) | d + n/86400 | |
| DATEDIFF(YEAR, a, b) | EXTRACT(YEAR FROM b) - EXTRACT(YEAR FROM a) | |
| DATEDIFF(MONTH, a, b) | MONTHS_BETWEEN(b, a) | |
| DATEDIFF(DAY, a, b) | TRUNC(b) - TRUNC(a) | |
| DATEPART(part, d) | EXTRACT(part FROM d) | |
| YEAR/MONTH/DAY(d) | EXTRACT(YEAR/MONTH/DAY FROM d) | |

### CONVERT 样式转换

| 样式号 | SQL Server 格式 | DM8 格式 |
|--------|-----------------|----------|
| 0 | Mon DD YYYY HH:MIAM/PM | YYYY-MM-DD HH24:MI:SS |
| 1 | MM/DD/YY | MM/DD/YY |
| 101 | MM/DD/YYYY | MM/DD/YYYY |
| 103 | DD/MM/YYYY | DD/MM/YYYY |
| 112 | YYYYMMDD | YYYYMMDD |
| 120 | YYYY-MM-DD HH:MI:SS | YYYY-MM-DD HH24:MI:SS |
| 121 | YYYY-MM-DD HH:MI:SS | YYYY-MM-DD HH24:MI:SS |

### 系统函数映射（4条）

| SQL Server | DM8 |
|------------|-----|
| @@IDENTITY | IDENTITY() |
| SCOPE_IDENTITY() | IDENTITY() |
| @@ERROR | SQLCODE |
| @@ROWCOUNT | SQL%ROWCOUNT |
| DB_NAME() | SYS_CONTEXT('USERENV','DB_NAME') |
| HOST_NAME() | SYS_CONTEXT('USERENV','HOST') |
| SUSER_NAME() | SYS_CONTEXT('USERENV','SESSION_USER') |

### 语法结构转换（18条）

| SQL Server | DM8 | 说明 |
|------------|-----|------|
| SELECT TOP N | FETCH FIRST N ROWS ONLY | |
| [表名] / [列名] | "表名" / "列名" | 方括号改双引号 |
| OUTER APPLY | LEFT JOIN LATERAL | |
| CROSS APPLY | CROSS JOIN LATERAL | |
| WITH (NOLOCK) | (已移除) | DM8 不支持此提示 |
| SET NOCOUNT ON | (已移除) | DM8 无需此设置 |
| CREATE VIEW/PROC/FUNC | CREATE OR REPLACE VIEW/PROC/FUNC | |
| BEGIN TRY...END TRY | EXCEPTION WHEN OTHERS THEN | |
| ELSE IF | ELSIF | |
| WHILE...BEGIN...END | LOOP...END LOOP | |
| SET @var = value | var := value | 赋值语法 |
| DECLARE @var TYPE | var TYPE | 声明语法 |
| PRINT msg | DBMS_OUTPUT.PUT_LINE(msg) | |
| RAISERROR(msg, ...) | RAISE_APPLICATION_ERROR(-20000, msg) | |
| EXEC(@sql) | EXECUTE IMMEDIATE sql | 动态 SQL |
| DECLARE CURSOR FOR | FOR rec IN (SELECT) LOOP | 游标转换提示 |
| #临时表 | 全局临时表 TEMP_ | |
| BEGIN TRANSACTION | BEGIN | |
| SAVE TRANSACTION name | SAVEPOINT name | |

---

## 📁 项目结构

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

---

## 📡 API 接口

### 连接管理
| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/connection/test | 测试数据库连接 |
| POST | /api/connection/databases | 获取数据库列表 |

### 数据库对象
| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/objects/{database}/tree | 获取对象树 |
| POST | /api/objects/{database}/views | 获取视图列表 |
| POST | /api/objects/{database}/functions | 获取函数列表 |
| POST | /api/objects/{database}/procedures | 获取存储过程列表 |
| POST | /api/objects/{database}/{type}/{schema}/{name}/sql | 获取 SQL 定义 |
| POST | /api/objects/{database}/test | 测试对象连接 |
| POST | /api/objects/{database}/debug | 调试：对象类型统计 |

### SQL 转换
| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/convert/single | 转换单个 SQL |
| POST | /api/convert/batch | 批量转换 |
| GET | /api/convert/rules | 获取转换规则列表 |
| POST | /api/convert/export | 导出转换结果（ZIP） |

### 示例数据
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/sample/objects | 获取示例对象列表 |
| POST | /api/sample/convert | 转换所有示例 |
| POST | /api/sample/convert/{index} | 转换单个示例 |

### 测试接口
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/test/conversion | 运行转换测试用例（38个） |
| POST | /api/test/convert | 测试单个 SQL 转换 |

### 健康检查
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/health | 服务健康检查 |

---

## 🧪 测试

### 运行转换测试

```bash
# 启动后端后访问
curl http://localhost:5000/api/test/conversion
```

### 测试结果

- 总测试用例：38
- 通过：38
- 失败：0
- **通过率：100%**

### 测试覆盖

| 类别 | 用例数 | 通过率 |
|------|--------|--------|
| 数据类型转换 | 5 | 100% |
| 内置函数转换 | 6 | 100% |
| 日期函数转换 | 5 | 100% |
| 语法结构转换 | 7 | 100% |
| 存储过程语法 | 6 | 100% |
| 系统函数转换 | 3 | 100% |
| 其他转换 | 6 | 100% |

---

## 📊 项目统计

| 指标 | 数值 |
|------|------|
| 后端文件 | 13 个 |
| 前端文件 | 15 个 |
| 后端代码 | 3,065 行 |
| 前端代码 | 2,481 行 |
| 总代码 | 5,546 行 |
| 转换规则 | 53 条 |
| 测试用例 | 38 个 |
| 测试通过率 | 100% |

---

## ⚠️ 已知局限性

| 局限 | 影响 | 建议 |
|------|------|------|
| 字符串内关键字可能被误替换 | 低 | 已实现保护机制，可按需启用 |
| 复杂嵌套结构解析不完整 | 低 | 大部分场景可正确处理 |
| 游标需要手动调整 | 低 | 已添加转换提示 |
| 表变量需手动处理 | 低 | 已添加转换提示 |

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT
