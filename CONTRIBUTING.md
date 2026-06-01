# 贡献指南

感谢您对 SQL2DM8 项目的关注！我们欢迎各种形式的贡献。

## 🤝 如何贡献

### 报告 Bug

1. 在 [Issues](https://github.com/your-username/SQL2DM8/issues) 中搜索是否已有相同问题
2. 如果没有，创建一个新的 Issue
3. 请包含以下信息：
   - 问题描述
   - 复现步骤
   - 期望行为
   - 实际行为
   - 环境信息（OS、.NET 版本、Node.js 版本）

### 提交功能建议

1. 在 [Issues](https://github.com/your-username/SQL2DM8/issues) 中创建新 Issue
2. 标题以 `[Feature]` 开头
3. 详细描述您的需求和使用场景

### 提交代码

1. Fork 本仓库
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 📋 开发规范

### 代码风格

#### 后端 (C#)
- 遵循 [C# 编码规范](https://docs.microsoft.com/zh-cn/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- 使用 PascalCase 命名公共成员
- 使用 camelCase 命名局部变量和参数
- 添加必要的注释

#### 前端 (TypeScript/React)
- 使用 ESLint 进行代码检查
- 组件使用 PascalCase 命名
- 使用函数组件和 Hooks
- Props 需要定义 TypeScript 类型

### 提交信息规范

使用 [Conventional Commits](https://www.conventionalcommits.org/) 规范：

```
<type>(<scope>): <subject>

<body>

<footer>
```

类型（type）：
- `feat`: 新功能
- `fix`: 修复 Bug
- `docs`: 文档更新
- `style`: 代码格式调整
- `refactor`: 重构
- `test`: 测试相关
- `chore`: 构建/工具相关

示例：
```
feat(conversion): 添加 CONVERT 样式转换支持

- 支持 30+ 种日期格式样式
- 自动转换为 TO_CHAR/TO_DATE

Closes #123
```

### 分支管理

- `main`: 稳定版本
- `develop`: 开发版本
- `feature/*`: 功能分支
- `fix/*`: 修复分支
- `release/*`: 发布分支

## 🧪 测试

### 运行测试

```bash
# 后端测试
cd backend
dotnet test

# 前端测试
cd frontend
npm test
```

### 提交前检查

1. 确保所有测试通过
2. 确保代码没有编译错误
3. 确保新功能有对应的测试用例

## 📝 文档

- 更新 README.md（如果添加新功能）
- 更新 PROJECT.md（如果修改架构）
- 添加代码注释（特别是公共 API）

## ❓ 问题

如有任何问题，请在 [Issues](https://github.com/your-username/SQL2DM8/issues) 中提问。

## 📜 行为准则

- 尊重每一位贡献者
- 接受建设性批评
- 专注于对社区最有利的事情
- 对他人表示同理心
