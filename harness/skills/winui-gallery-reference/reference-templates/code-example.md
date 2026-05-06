# 代码示例模板

## 基本用法

### XAML 示例
```xml
<!-- 基本组件用法 -->
<ComponentName Property="Value">
    <!-- 子内容 -->
</ComponentName>
```

### C# 代码
```csharp
// 基本代码逻辑
public partial class ExamplePage : Page
{
    public ExamplePage()
    {
        this.InitializeComponent();
    }
    
    // 事件处理
    private void OnEvent(object sender, RoutedEventArgs e)
    {
        // 处理逻辑
    }
}
```

## 常用属性

### 外观属性
```xml
<ComponentName 
    Width="200"
    Height="100"
    Margin="10"
    Padding="5"
    HorizontalAlignment="Center"
    VerticalAlignment="Center"
    Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
    Foreground="{ThemeResource TextFillColorPrimaryBrush}"
    BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
    BorderThickness="1"
    CornerRadius="4"/>
```

### 交互属性
```xml
<ComponentName 
    IsEnabled="True"
    IsReadOnly="False"
    IsHitTestVisible="True"
    AllowDrop="True"
    FocusState="Unfocused"
    TabIndex="0"/>
```

## 事件处理

### 常用事件
```csharp
// 点击事件
private void OnClick(object sender, RoutedEventArgs e)
{
    // 处理点击
}

// 选择变化事件
private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    // 处理选择变化
}

// 文本变化事件
private void OnTextChanged(object sender, TextChangedEventArgs e)
{
    // 处理文本变化
}

// 加载完成事件
private void OnLoaded(object sender, RoutedEventArgs e)
{
    // 处理加载完成
}
```

## 数据绑定

### 基本绑定
```xml
<ComponentName Content="{Binding PropertyName}"/>
```

### 带转换器的绑定
```xml
<ComponentName Content="{Binding PropertyName, Converter={StaticResource MyConverter}}"/>
```

### 双向绑定
```xml
<TextBox Text="{Binding PropertyName, Mode=TwoWay}"/>
```

## 样式与模板

### 内联样式
```xml
<ComponentName Style="{StaticResource MyStyle}"/>
```

### 自定义样式
```xml
<Page.Resources>
    <Style x:Key="MyStyle" TargetType="ComponentName">
        <Setter Property="Background" Value="Red"/>
        <Setter Property="Foreground" Value="White"/>
    </Style>
</Page.Resources>
```

### 控件模板
```xml
<ControlTemplate TargetType="ComponentName">
    <Grid>
        <!-- 模板内容 -->
    </Grid>
</ControlTemplate>
```

## 代码示例

### 完整页面示例
```xml
<Page
    x:Class="MyApp.ExamplePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <CommandBar Grid.Row="0">
            <AppBarButton Icon="Save" Label="保存"/>
            <AppBarButton Icon="Undo" Label="撤销"/>
        </CommandBar>
        
        <ScrollViewer Grid.Row="1">
            <StackPanel Padding="16" Spacing="8">
                <!-- 内容 -->
            </StackPanel>
        </ScrollViewer>
    </Grid>
</Page>
```

### 完整代码示例
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MyApp
{
    public sealed partial class ExamplePage : Page
    {
        public ExamplePage()
        {
            this.InitializeComponent();
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // 页面导航到时的处理
        }
        
        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            // 保存逻辑
        }
        
        private void OnUndoClick(object sender, RoutedEventArgs e)
        {
            // 撤销逻辑
        }
    }
}
```