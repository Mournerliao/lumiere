# GitHub 内容获取指南

## 概述
本指南说明如何使用 `webfetch` 工具从 WinUI Gallery GitHub 仓库获取组件示例代码。

## GitHub API 使用

### 获取文件内容
```
URL: https://api.github.com/repos/microsoft/WinUI-Gallery/contents/{path}
```

### 获取目录内容
```
URL: https://api.github.com/repos/microsoft/WinUI-Gallery/contents/{path}
```

### 搜索代码
```
URL: https://api.github.com/search/code?q={query}+repo:microsoft/WinUI-Gallery
```

## WinUI Gallery 结构

### 主要目录
- `WinUIGallery/Samples/ControlPages/` - 控件示例页面
- `WinUIGallery/Samples/Data/` - 示例数据
- `WinUIGallery/Samples/SampleCode/` - 示例代码
- `WinUIGallery/Styles/` - 样式文件

### 文件命名规则
- 控件页面：`{ComponentName}Page.xaml` 和 `{ComponentName}Page.xaml.cs`
- 示例代码：`{ComponentName}Example.xaml` 和 `{ComponentName}Example.xaml.cs`

## 获取步骤

### 步骤1：确定组件名称
从用户请求中识别组件名称，例如：
- "NavigationView" → `NavigationViewPage.xaml`
- "Button" → `ButtonPage.xaml`
- "ListView" → `ListViewPage.xaml`

### 步骤2：获取 XAML 文件
```bash
# 使用 webfetch 获取 XAML 文件
webfetch "https://raw.githubusercontent.com/microsoft/WinUI-Gallery/main/WinUIGallery/Samples/ControlPages/{ComponentName}Page.xaml"
```

### 步骤3：获取 C# 文件
```bash
# 使用 webfetch 获取 C# 文件
webfetch "https://raw.githubusercontent.com/microsoft/WinUI-Gallery/main/WinUIGallery/Samples/ControlPages/{ComponentName}Page.xaml.cs"
```

### 步骤4：获取样式文件（如果需要）
```bash
# 获取相关样式
webfetch "https://raw.githubusercontent.com/microsoft/WinUI-Gallery/main/WinUIGallery/Styles/{StyleName}.xaml"
```

## 内容解析

### 提取 XAML 示例
从 XAML 文件中提取：
- 基本用法示例
- 属性设置示例
- 事件绑定示例
- 样式定义示例

### 提取 C# 代码
从 C# 文件中提取：
- 事件处理程序
- 数据绑定逻辑
- 初始化代码
- 辅助方法

### 提取样式信息
从样式文件中提取：
- 默认样式
- 主题资源
- 自定义样式示例

## 示例：获取 NavigationView 参考

### 1. 获取 XAML 文件
```bash
webfetch "https://raw.githubusercontent.com/microsoft/WinUI-Gallery/main/WinUIGallery/Samples/ControlPages/NavigationViewPage.xaml"
```

### 2. 获取 C# 文件
```bash
webfetch "https://raw.githubusercontent.com/microsoft/WinUI-Gallery/main/WinUIGallery/Samples/ControlPages/NavigationViewPage.xaml.cs"
```

### 3. 解析内容
从文件中提取：
- NavigationView 的基本用法
- 导航菜单的配置
- 事件处理示例
- 样式定制示例

## 错误处理

### 文件不存在
- **问题**：请求的文件不存在
- **解决**：检查文件名是否正确，尝试搜索类似文件

### 网络错误
- **问题**：无法访问 GitHub
- **解决**：检查网络连接，使用缓存内容

### 权限错误
- **问题**：API 调用限制
- **解决**：等待一段时间后重试，或使用缓存

## 缓存策略

### 本地缓存
- 缓存常用组件示例
- 定期更新缓存内容
- 支持离线访问

### 缓存更新
- 每天检查一次更新
- 手动触发更新
- 版本控制缓存内容

## 最佳实践

### 获取策略
1. **按需获取**：只获取需要的组件
2. **批量获取**：一次获取多个相关文件
3. **增量更新**：只更新变化的部分

### 内容处理
1. **提取关键代码**：只提取示例代码
2. **格式化输出**：整理代码格式
3. **添加注释**：解释代码功能

### 错误处理
1. **重试机制**：失败时自动重试
2. **降级方案**：使用缓存内容
3. **用户提示**：提供有用的错误信息

## 工具集成

### 与 webfetch 集成
```markdown
# 使用 webfetch 获取内容
webfetch "https://raw.githubusercontent.com/microsoft/WinUI-Gallery/main/WinUIGallery/Samples/ControlPages/{ComponentName}Page.xaml"
```

### 与 skill 集成
```markdown
# 在 skill 中使用
1. 解析用户请求
2. 确定组件名称
3. 获取 GitHub 内容
4. 解析和格式化
5. 输出参考信息
```

## 示例代码

### 获取 Button 示例
```markdown
用户请求：如何使用 Button？

1. 确定组件：Button
2. 获取文件：
   - ButtonPage.xaml
   - ButtonPage.xaml.cs
3. 解析内容：
   - 基本用法
   - 事件处理
   - 样式定制
4. 输出参考
```

### 获取 NavigationView 示例
```markdown
用户请求：如何实现导航菜单？

1. 确定组件：NavigationView
2. 获取文件：
   - NavigationViewPage.xaml
   - NavigationViewPage.xaml.cs
3. 解析内容：
   - 导航配置
   - 菜单项定义
   - 事件处理
4. 输出参考
```