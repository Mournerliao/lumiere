# WinUI Gallery Reference Skill

这个 skill 提供从官方 WinUI Gallery 获取 WinUI 3 组件参考的能力。

## 功能特性

- **智能匹配**：根据功能描述自动匹配相关组件
- **完整参考**：提供代码示例、用法说明和最佳实践
- **在线获取**：实时从 GitHub 仓库获取最新示例

## 使用方法

### 直接调用
```
@winui-gallery-reference 如何使用 NavigationView？
```

### 功能描述
```
@winui-gallery-reference 如何实现导航菜单？
```

### 组件查询
```
@winui-gallery-reference Button 组件的用法
```

## 工作流程

1. **解析请求**：理解用户需要什么组件或功能
2. **智能匹配**：将功能描述映射到具体组件
3. **获取参考**：从 WinUI Gallery 获取代码示例
4. **格式化输出**：提供结构化的参考信息

## 输出格式

### 参考文档结构
```markdown
# [组件名称] 参考

## 概述
- 功能描述
- 适用场景

## 基本用法
### XAML 示例
### C# 代码

## 高级用法
### 自定义样式
### 数据绑定

## 最佳实践
- 使用建议
- 性能优化
- 无障碍支持

## 相关组件
- 相关组件列表

## 参考链接
- 官方文档
- GitHub 示例
```

## 组件映射

### 导航与布局
- NavigationView - 主导航菜单
- BreadcrumbBar - 面包屑导航
- TabView - 标签页导航
- SplitView - 分栏布局

### 数据输入
- TextBox - 文本输入
- ComboBox - 下拉选择
- CheckBox - 复选框
- RadioButton - 单选按钮
- DatePicker - 日期选择
- TimePicker - 时间选择

### 数据展示
- ListView - 列表展示
- GridView - 网格展示
- TreeView - 树形结构
- ContentDialog - 详细信息

### 命令与操作
- CommandBar - 工具栏
- Button - 按钮
- AppBarButton - 应用栏按钮
- MenuFlyout - 上下文菜单

### 反馈与状态
- ProgressBar - 进度条
- ProgressRing - 进度环
- InfoBar - 信息栏
- ToolTip - 工具提示

## 文件结构

```
winui-gallery-reference/
├── SKILL.md                    # 入口文件
├── workflow.md                 # 工作流程
├── component-mapping.md        # 组件映射表
├── README.md                   # 本文件
├── reference-templates/        # 参考模板
│   ├── code-example.md         # 代码示例模板
│   ├── usage-guide.md          # 用法说明模板
│   └── best-practices.md       # 最佳实践模板
└── utils/                      # 工具脚本
    └── fetch-github-content.md # GitHub 内容获取指南
```

## 集成方式

### 独立使用
- 直接调用 skill 获取组件参考
- 手动查询特定组件用法

### 与其他 skill 集成
- 可被 `bmad-quick-dev` 调用
- 在实现 WinUI 功能时自动获取参考

## 技术实现

### 内容来源
- GitHub: microsoft/WinUI-Gallery
- 文件路径：`WinUIGallery/Samples/ControlPages/`

### 获取方式
- 使用 `webfetch` 工具在线获取
- 支持缓存机制提高效率

### 智能匹配
- 关键词匹配
- 语义分析
- 上下文理解

## 示例

### 示例1：直接组件查询
```
用户：如何使用 NavigationView？
AI：调用 winui-gallery-reference skill，获取 NavigationView 的完整参考
```

### 示例2：功能需求
```
用户：如何实现导航菜单？
AI：
1. 智能匹配：NavigationView, BreadcrumbBar
2. 获取参考：从 WinUI Gallery 获取示例
3. 提供指导：完整的实现指南
```

### 示例3：实现参考
```
用户：我需要实现一个设置页面
AI：
1. 智能匹配：SettingsCard, Expander, StackPanel
2. 获取参考：相关组件的用法
3. 提供方案：完整的设置页面实现方案
```

## 注意事项

### 网络依赖
- 需要网络访问 GitHub
- 建议实现缓存机制

### 内容更新
- WinUI Gallery 内容可能更新
- 定期检查示例代码的适用性

### 版本兼容
- 注意 WinUI 3 版本差异
- 检查 API 兼容性

## 扩展性

### 添加新组件
1. 在 `component-mapping.md` 中添加映射
2. 更新参考模板
3. 测试获取逻辑

### 优化匹配算法
1. 改进关键词识别
2. 增强语义分析
3. 优化上下文理解

### 增强输出格式
1. 添加更多示例
2. 提供交互式示例
3. 支持代码片段复制