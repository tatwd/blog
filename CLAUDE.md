# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 提供在本仓库中工作的指导。

## 项目概述

这是一个基于 **C# .NET 9.0** 的静态站点生成器，用于生成个人博客。主要功能包括：
- 将 Markdown 文件处理成静态 HTML
- 使用 Razor 模板
- 支持代码语法高亮
- 生成 RSS/Atom 订阅源
- 支持部署到 Vercel 和 Deno Deploy

## 常用命令

### 构建和运行

```bash
# 生成静态站点到 dist/ 目录
dotnet run --project src/blog.csproj --cwd ./

# 或使用快捷脚本：
./build.sh          # Linux/Unix 构建脚本
./dev.ps1           # Windows 开发模式（支持热重载）

# 预览生成的站点
dotnet tool restore
dotnet serve --directory dist
```

### 测试

```bash
# 运行所有测试
dotnet test -c Release --no-restore

# 开发时运行测试（监听模式）
dotnet test --watch
```

### 开发模式（热重载）

```bash
# 热重载会自动监听 posts、spa 和 theme 目录的变化
dotnet watch --project src/blog.csproj -- run -- --dev --cwd ./
```

## 代码架构

### 核心组件

**入口文件**: `src/Program.cs`
- 静态站点生成的主要逻辑
- 处理命令行参数
- 协调整个构建流程

**关键类**:
- `BlogConfig.cs` - 博客配置（标题、作者、链接等）
- `Post.cs` - 博客文章数据模型
- `PostFrontMatter.cs` - YAML 前言解析器
- `MarkdownRenderer.cs` - Markdown 处理（基于 Markdig）
- `RazorRenderer.cs` - Razor 模板渲染引擎
- `MyPrismCodeBlockRenderer.cs` - 使用 Prism 的语法高亮

**模板系统**: `theme/templates/`
- `index.cshtml` - 首页模板
- `post.cshtml` - 文章页模板
- `spa.cshtml` - 静态页面模板（用于 About、Tools 等页面）
- `tag.cshtml` - 标签页模板

### 内容结构

**文章**: `posts/`
- 按主题分类的子目录（如 `ajax-dotnet/`、`lua-in-redis/`）
- 每篇文章都是带 YAML 前言的 Markdown 文件
- 前言包含：标题、日期、标签、草稿状态、语言

**静态页面**: `spa/`
- 使用 "spa" 模板的额外页面
- 包括：prompts.md、tips.md、tools.md、about.md

**主题资源**: `theme/`
- `styles/` - CSS 文件（生产环境会压缩）
- `scripts/` - JavaScript 文件
- `fonts/` - 字体文件
- 静态文件：avatar.jpg、favicon.ico、robots.txt

### 构建流程

1. **解析配置**: 读取博客配置和命令行参数
2. **处理文章**: 加载并解析 `posts/` 中的所有 Markdown 文件
3. **渲染模板**: 使用 Razor 模板生成 HTML
4. **生成页面**: 创建首页、文章页、标签页和 SPA 页面
5. **处理资源**: 复制并压缩 CSS/JS（生产环境）
6. **生成订阅源**: 创建 RSS 和 Atom 订阅源
7. **输出**: 所有生成的文件都在 `dist/` 目录

### 部署

根据提交信息部署到不同平台：

- **预览部署到 Vercel**: 提交信息以 `preview:` 开头
- **生产部署到 Vercel**: 提交信息以 `release:` 开头
- **生产部署到 Deno Deploy**: 提交信息以 `release:` 开头

CI/CD 流程在部署前会运行测试，测试必须通过才能部署。

## 重要说明

- 需要 **.NET 9.0** SDK
- **草稿文章**在生产构建中会被排除（不使用 `--dev` 标志时）
- **热重载**会自动监听文章、SPA 页面和主题文件的变化
- 支持 **干净的 URL**（支持尾部斜杠）
- **多语言支持** - 文章可以在前言中指定语言
- 博客配置为中文内容，但支持任何语言
- 生产构建会压缩资源（CSS/JS）
