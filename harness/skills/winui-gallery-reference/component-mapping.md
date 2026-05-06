# WinUI 3 Component Mapping

## 概述
本文件提供从功能描述到 WinUI 3 组件的智能映射，帮助快速定位合适的组件。

## 导航与布局

### 应用导航
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 主导航菜单 | NavigationView | SplitView | 应用主导航，侧边栏菜单 |
| 面包屑导航 | BreadcrumbBar | - | 层级路径导航 |
| 标签页导航 | TabView | Pivot | 多文档界面，标签式导航 |
| 步骤导航 | BreadcrumbBar | NavigationView | 向导式流程 |

### 页面布局
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 网格布局 | Grid | - | 复杂布局，行列对齐 |
| 线性布局 | StackPanel | - | 水平或垂直排列元素 |
| 自适应布局 | AdaptiveGridView | WrapPanel | 响应式网格 |
| 可折叠面板 | Expander | - | 可展开/折叠的内容区域 |
| 分栏布局 | SplitView | - | 主从视图，侧边栏 |

## 数据输入

### 文本输入
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 单行文本输入 | TextBox | - | 基本文本输入 |
| 多行文本输入 | TextBox (AcceptsReturn) | RichEditBox | 多行文本编辑 |
| 搜索框 | AutoSuggestBox | TextBox | 搜索功能，自动建议 |
| 密码输入 | PasswordBox | - | 密码字段 |

### 选择输入
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 下拉选择 | ComboBox | - | 单选下拉列表 |
| 多选列表 | ListBox (SelectionMode=Multiple) | ListView | 多选列表 |
| 开关切换 | ToggleSwitch | CheckBox | 布尔值切换 |
| 复选框 | CheckBox | - | 多选选项 |
| 单选按钮 | RadioButton | - | 单选选项组 |

### 日期时间
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 日期选择 | DatePicker | CalendarDatePicker | 日期选择器 |
| 时间选择 | TimePicker | - | 时间选择器 |
| 日期范围 | CalendarView | - | 日期范围选择 |

### 数值输入
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 数值输入 | NumberBox | TextBox | 数值输入，带验证 |
| 滑块选择 | Slider | - | 范围值选择 |
| 评分 | RatingControl | - | 星级评分 |

## 数据展示

### 列表与网格
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 垂直列表 | ListView | ListBox | 基本列表展示 |
| 网格列表 | GridView | - | 图片/卡片网格 |
| 树形结构 | TreeView | - | 层级数据展示 |
| 虚拟化列表 | ListView (IsItemClickEnabled) | - | 大数据量列表 |

### 详细信息
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 详细信息 | ContentDialog | Flyout | 详细信息弹窗 |
| 工具提示 | ToolTip | - | 悬停提示 |
| 信息徽章 | InfoBadge | - | 状态指示 |
| 信息栏 | InfoBar | - | 消息通知 |

### 媒体与图形
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 图片展示 | Image | - | 图片显示 |
| 图标 | FontIcon / SymbolIcon | - | 图标显示 |
| 媒体播放 | MediaElement | - | 视频/音频播放 |
| 绘图 | Canvas | InkCanvas | 自定义绘图 |

## 命令与操作

### 工具栏
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 主工具栏 | CommandBar | - | 应用主命令栏 |
| 上下文菜单 | MenuFlyout | - | 右键菜单 |
| 应用栏按钮 | AppBarButton | Button | 工具栏按钮 |
| 切换按钮 | AppBarToggleButton | ToggleButton | 工具栏切换按钮 |

### 按钮
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 标准按钮 | Button | - | 基本操作按钮 |
| 下拉按钮 | DropDownButton | SplitButton | 带下拉菜单的按钮 |
| 超链接 | HyperlinkButton | - | 链接按钮 |
| 重复按钮 | RepeatButton | - | 长按重复操作 |

## 反馈与状态

### 进度指示
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 确定进度 | ProgressBar | - | 已知进度的加载 |
| 不确定进度 | ProgressRing | - | 未知进度的加载 |
| 评分 | RatingControl | - | 星级评分 |

### 状态显示
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 状态信息 | InfoBar | - | 状态消息显示 |
| 徽章 | InfoBadge | - | 通知计数/状态 |
| 加载状态 | ProgressRing | - | 加载中状态 |

## 设置与配置

### 设置页面
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 设置卡片 | SettingsCard | - | 设置项卡片 |
| 设置分组 | Expander | - | 可折叠设置组 |
| 设置页面 | SettingsExpander | - | 设置页面布局 |

## 特殊场景

### HDR 与图形
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 颜色选择 | ColorPicker | - | 颜色选择器 |
| 图片预览 | Image | - | 图片预览 |
| 画布绘制 | InkCanvas | Canvas | 手写/绘图 |

### 捕获工具相关
| 功能描述 | 推荐组件 | 备选组件 | 使用场景 |
|---------|---------|---------|---------|
| 区域选择 | Canvas + Pointer 事件 | - | 截图区域选择 |
| 工具栏 | CommandBar | - | 截图工具栏 |
| 标注工具 | InkCanvas | Canvas | 标注绘制 |
| 预览窗口 | Image + ToolTip | - | 截图预览 |

## 组件组合模式

### 主从视图
```xml
<SplitView>
    <SplitView.Pane>
        <!-- 导航菜单 -->
        <NavigationView />
    </SplitView.Pane>
    <SplitView.Content>
        <!-- 主内容 -->
    </SplitView.Content>
</SplitView>
```

### 设置页面
```xml
<ScrollViewer>
    <StackPanel>
        <SettingsCard Header="基本设置">
            <!-- 设置内容 -->
        </SettingsCard>
        <Expander Header="高级设置">
            <!-- 高级设置内容 -->
        </Expander>
    </StackPanel>
</ScrollViewer>
```

### 工具栏 + 内容
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <CommandBar Grid.Row="0">
        <AppBarButton Icon="Save"/>
        <AppBarButton Icon="Undo"/>
    </CommandBar>
    <ContentPresenter Grid.Row="1"/>
</Grid>
```

## 使用建议

### 选择组件的原则
1. **原生优先**：优先使用 WinUI 3 原生组件
2. **功能匹配**：选择最符合功能需求的组件
3. **一致性**：保持应用内组件使用的一致性
4. **无障碍**：确保组件支持无障碍访问

### 性能考虑
1. **虚拟化**：大数据量使用虚拟化列表
2. **延迟加载**：非关键内容延迟加载
3. **缓存**：适当使用数据缓存
4. **异步**：耗时操作使用异步处理

### 常见错误避免
1. **过度自定义**：避免过度自定义原生组件
2. **布局嵌套**：避免过深的布局嵌套
3. **硬编码**：避免硬编码尺寸和颜色
4. **内存泄漏**：正确处理事件订阅和资源释放