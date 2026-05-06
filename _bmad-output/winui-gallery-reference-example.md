# NavigationView 参考

## 概述
- **功能描述**：NavigationView 是 WinUI 3 中的主要导航控件，用于创建应用主导航结构
- **适用场景**：应用主导航、侧边栏菜单、多页面导航
- **版本要求**：Windows App SDK 1.0+

## 基本用法

### XAML 示例 - 左侧导航
```xml
<NavigationView
    x:Name="nvSample"
    Header="This is Header Text"
    PaneDisplayMode="Left"
    SelectionChanged="NavigationView_SelectionChanged">
    
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Icon="Home" Tag="SamplePage1" />
        <NavigationViewItem Content="Account" Icon="Contact" Tag="SamplePage2" />
        <NavigationViewItem Content="Settings" Icon="Settings" Tag="SamplePage3" />
    </NavigationView.MenuItems>
    
    <Frame x:Name="contentFrame" />
</NavigationView>
```

### XAML 示例 - 顶部导航
```xml
<NavigationView
    x:Name="nvSample"
    Header="This is Header Text"
    PaneDisplayMode="Top"
    SelectionChanged="NavigationView_SelectionChanged">
    
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Tag="SamplePage1" />
        <NavigationViewItem Content="Account" Tag="SamplePage2" />
        <NavigationViewItem Content="Settings" Tag="SamplePage3" />
    </NavigationView.MenuItems>
    
    <Frame x:Name="contentFrame" />
</NavigationView>
```

### C# 代码
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MyApp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            
            // 设置默认选中项
            nvSample.SelectedItem = nvSample.MenuItems[0];
        }
        
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                string pageTag = (string)selectedItem.Tag;
                
                // 根据 Tag 导航到对应页面
                switch (pageTag)
                {
                    case "SamplePage1":
                        contentFrame.Navigate(typeof(HomePage));
                        break;
                    case "SamplePage2":
                        contentFrame.Navigate(typeof(AccountPage));
                        break;
                    case "SamplePage3":
                        contentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }
    }
}
```

## 高级用法

### 自适应导航模式
```xml
<NavigationView
    x:Name="nvSample"
    PaneDisplayMode="Auto"
    SelectionChanged="NavigationView_SelectionChanged">
    
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Icon="Home" Tag="SamplePage1" />
        <NavigationViewItem Content="Account" Icon="Contact" Tag="SamplePage2" />
        <NavigationViewItem Content="Settings" Icon="Settings" Tag="SamplePage3" />
    </NavigationView.MenuItems>
    
    <Frame x:Name="contentFrame" />
</NavigationView>
```

### 层级导航
```xml
<NavigationView
    x:Name="nvSample"
    PaneDisplayMode="Left"
    SelectionChanged="NavigationView_SelectionChanged">
    
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Icon="Home" Tag="SamplePage1" />
        <NavigationViewItem Content="Account" Icon="Contact" Tag="SamplePage2">
            <NavigationViewItem.MenuItems>
                <NavigationViewItem Content="Mail" Icon="Mail" Tag="SamplePage3" />
                <NavigationViewItem Content="Calendar" Icon="Calendar" Tag="SamplePage4" />
            </NavigationViewItem.MenuItems>
        </NavigationViewItem>
        <NavigationViewItem Content="Settings" Icon="Settings" Tag="SamplePage5" />
    </NavigationView.MenuItems>
    
    <Frame x:Name="contentFrame" />
</NavigationView>
```

### 页脚菜单项
```xml
<NavigationView
    x:Name="nvSample"
    PaneDisplayMode="Left"
    SelectionChanged="NavigationView_SelectionChanged">
    
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Icon="Home" Tag="SamplePage1" />
        <NavigationViewItem Content="Account" Icon="Contact" Tag="SamplePage2" />
    </NavigationView.MenuItems>
    
    <NavigationView.FooterMenuItems>
        <NavigationViewItem Content="Settings" Icon="Settings" Tag="SamplePage3" />
        <NavigationViewItem Content="Help" Icon="Help" Tag="SamplePage4" />
    </NavigationView.FooterMenuItems>
    
    <Frame x:Name="contentFrame" />
</NavigationView>
```

### 数据绑定
```xml
<NavigationView
    x:Name="nvSample"
    MenuItemsSource="{x:Bind Categories}"
    MenuItemTemplateSelector="{StaticResource selector}"
    SelectionChanged="NavigationView_SelectionChanged">
    
    <Frame x:Name="contentFrame" />
</NavigationView>
```

```csharp
// 数据模型
public class Category
{
    public string Name { get; set; }
    public Symbol Glyph { get; set; }
    public string Tooltip { get; set; }
}

// ViewModel
public ObservableCollection<Category> Categories { get; set; }

public MainPage()
{
    this.InitializeComponent();
    
    Categories = new ObservableCollection<Category>
    {
        new Category { Name = "Home", Glyph = Symbol.Home, Tooltip = "Home page" },
        new Category { Name = "Account", Glyph = Symbol.Contact, Tooltip = "Account page" },
        new Category { Name = "Settings", Glyph = Symbol.Settings, Tooltip = "Settings page" }
    };
}
```

## 最佳实践

### 使用建议
1. **选择合适的显示模式**：
   - `Left`：适合 5 个以上重要导航项
   - `Top`：适合较少导航项，强调内容
   - `Auto`：根据窗口宽度自动切换

2. **合理使用图标**：
   - 为每个导航项添加图标
   - 使用系统图标保持一致性
   - 图标应直观表示功能

3. **组织导航结构**：
   - 将常用功能放在前面
   - 使用分组和层级组织复杂导航
   - 页脚放置设置和帮助

### 性能优化
1. **延迟加载**：非关键页面延迟加载
2. **虚拟化**：大量导航项使用虚拟化
3. **缓存**：缓存已访问的页面

### 无障碍支持
1. **键盘导航**：支持键盘快捷键
2. **屏幕阅读器**：提供适当的标签
3. **焦点管理**：合理管理焦点顺序

## 相关组件

### 组合使用
- **Frame**：用于页面导航
- **AutoSuggestBox**：搜索功能
- **NavigationViewItem**：导航项
- **NavigationViewItemHeader**：分组标题

### 替代方案
- **SplitView**：更简单的分栏布局
- **TabView**：标签式导航
- **BreadcrumbBar**：面包屑导航

## 参考链接

### 官方文档
- [NavigationView 类](https://learn.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.controls.navigationview)
- [NavigationView 指南](https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview)

### GitHub 示例
- [WinUI Gallery - NavigationView 示例](https://github.com/microsoft/WinUI-Gallery/tree/main/WinUIGallery/Samples/ControlPages/NavigationViewPage.xaml)
- [WinUI Gallery - 源代码](https://github.com/microsoft/WinUI-Gallery)

### 相关资源
- [Fluent Design System](https://fluent2.microsoft.design/)
- [Windows App SDK 文档](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)

## 常见问题

### 问题1：导航项不显示
**原因**：未正确设置 MenuItems 或 MenuItemsSource
**解决**：检查 XAML 中的 MenuItems 定义或 C# 中的数据绑定

### 问题2：选择事件不触发
**原因**：SelectionChanged 事件处理程序未正确绑定
**解决**：检查事件名称和处理程序签名

### 问题3：页面导航失败
**原因**：Frame 或页面类型未正确配置
**解决**：检查 contentFrame.Navigate() 调用和页面类型

## 版本历史

### 最新版本
- 支持层级导航
- 改进无障碍支持
- 新增 SelectionFollowsFocus 属性

### 兼容性
- Windows 10 版本 1809 及更高版本
- Windows App SDK 1.0+