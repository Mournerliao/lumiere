# 最佳实践模板

## 设计原则

### 原生优先
- **优先使用 WinUI 3 原生组件**
- 避免过度自定义，保持平台一致性
- 使用 Fluent Design System 资源

### 功能优先
- 选择最符合功能需求的组件
- 考虑组件的扩展性和维护性
- 平衡功能复杂度和用户体验

### 无障碍优先
- 确保所有用户都能使用
- 支持键盘导航和屏幕阅读器
- 提供适当的替代文本

## 性能优化

### 虚拟化
```xml
<!-- 使用虚拟化列表 -->
<ListView 
    ItemsSource="{x:Bind LargeCollection}"
    SelectionMode="Single">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:Item">
            <!-- 简洁的模板 -->
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### 延迟加载
```csharp
// 延迟加载非关键内容
private async void OnLoaded(object sender, RoutedEventArgs e)
{
    await LoadCriticalDataAsync();
    
    // 延迟加载非关键内容
    await Task.Delay(100);
    await LoadNonCriticalDataAsync();
}
```

### 数据缓存
```csharp
// 缓存频繁访问的数据
private static readonly Dictionary<string, object> _cache = new();

private async Task<object> GetDataAsync(string key)
{
    if (_cache.TryGetValue(key, out var cached))
    {
        return cached;
    }
    
    var data = await FetchDataAsync(key);
    _cache[key] = data;
    return data;
}
```

### 异步操作
```csharp
// 使用异步避免UI阻塞
private async void OnButtonClick(object sender, RoutedEventArgs e)
{
    try
    {
        IsLoading = true;
        await LongRunningOperationAsync();
    }
    catch (Exception ex)
    {
        await ShowErrorAsync(ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}
```

## 内存管理

### 事件订阅
```csharp
// 正确订阅和取消订阅事件
public sealed partial class MyPage : Page
{
    public MyPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }
    
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
```

### 资源释放
```csharp
// 实现 IDisposable
public sealed partial class MyControl : UserControl, IDisposable
{
    private bool _disposed = false;
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // 释放资源
            _timer?.Dispose();
            _stream?.Dispose();
            
            _disposed = true;
        }
    }
}
```

## 布局优化

### 避免过度嵌套
```xml
<!-- 不推荐：过度嵌套 -->
<Grid>
    <StackPanel>
        <Grid>
            <StackPanel>
                <!-- 内容 -->
            </StackPanel>
        </Grid>
    </StackPanel>
</Grid>

<!-- 推荐：扁平化布局 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <!-- 内容 -->
</Grid>
```

### 合理使用尺寸
```xml
<!-- 不推荐：硬编码尺寸 -->
<Button Width="200" Height="50"/>

<!-- 推荐：自适应尺寸 -->
<Button HorizontalAlignment="Stretch"/>
```

## 样式与主题

### 使用主题资源
```xml
<!-- 使用主题资源 -->
<Button 
    Background="{ThemeResource ButtonBackground}"
    Foreground="{ThemeResource ButtonForeground}"
    BorderBrush="{ThemeResource ButtonBorderBrush}"/>
```

### 自定义样式
```xml
<!-- 定义可重用样式 -->
<Style x:Key="PrimaryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{ThemeResource AccentFillColorDefaultBrush}"/>
    <Setter Property="Foreground" Value="{ThemeResource TextOnAccentFillColorPrimaryBrush}"/>
    <Setter Property="CornerRadius" Value="4"/>
</Style>

<!-- 使用样式 -->
<Button Style="{StaticResource PrimaryButtonStyle}"/>
```

## 无障碍支持

### 基本无障碍
```xml
<!-- 提供替代文本 -->
<Image Source="image.png" AutomationProperties.Name="产品图片"/>

<!-- 设置标签 -->
<TextBox AutomationProperties.LabeledBy="{x:Bind EmailLabel}"/>
<TextBlock x:Name="EmailLabel" Text="电子邮件"/>
```

### 键盘导航
```xml
<!-- 支持键盘导航 -->
<Button 
    TabIndex="0"
    IsTabStop="True"
    AllowFocusOnInteraction="True"
    Click="OnButtonClick"/>
```

### 屏幕阅读器支持
```csharp
// 动态更新无障碍信息
private void UpdateStatus(string message)
{
    StatusText.Text = message;
    AutomationProperties.SetName(StatusText, message);
    
    // 通知屏幕阅读器
    var peer = FrameworkElementAutomationPeer.FromElement(StatusText);
    peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
}
```

## 错误处理

### 输入验证
```csharp
// 验证用户输入
private bool ValidateInput()
{
    if (string.IsNullOrEmpty(NameTextBox.Text))
    {
        ShowError("请输入姓名");
        NameTextBox.Focus(FocusState.Programmatic);
        return false;
    }
    
    if (!int.TryParse(AgeTextBox.Text, out var age) || age < 0 || age > 150)
    {
        ShowError("请输入有效的年龄");
        AgeTextBox.Focus(FocusState.Programmatic);
        return false;
    }
    
    return true;
}
```

### 异常处理
```csharp
// 全局异常处理
private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    e.Handled = true;
    
    // 记录错误
    Logger.LogError(e.Exception);
    
    // 显示用户友好的错误信息
    await ShowErrorAsync("发生意外错误，请稍后重试");
}
```

## 代码组织

### MVVM 模式
```csharp
// ViewModel
public class MainViewModel : INotifyPropertyChanged
{
    private string _title;
    
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### 命令模式
```csharp
// 使用 ICommand
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;
    
    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
    
    public void Execute(object parameter) => _execute();
    
    public event EventHandler CanExecuteChanged;
}
```

## 测试策略

### 单元测试
```csharp
// 测试 ViewModel
[TestClass]
public class MainViewModelTests
{
    [TestMethod]
    public void Title_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Title))
                propertyChanged = true;
        };
        
        // Act
        viewModel.Title = "New Title";
        
        // Assert
        Assert.IsTrue(propertyChanged);
        Assert.AreEqual("New Title", viewModel.Title);
    }
}
```

### UI 测试
```csharp
// UI 自动化测试
[TestClass]
public class MainPageTests
{
    [TestMethod]
    public async Task ClickButton_DisplaysMessage()
    {
        // Arrange
        var app = await Application.LaunchAsync(typeof(App));
        var page = app.MainWindow.Content as MainPage;
        
        // Act
        var button = page.FindName("MyButton") as Button;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        
        // Assert
        var message = page.FindName("MessageText") as TextBlock;
        Assert.AreEqual("Button clicked!", message.Text);
    }
}
```

## 发布与部署

### 资源管理
```xml
<!-- 使用资源管理 -->
<Page.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="ms-appx:///Styles/MyStyles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Page.Resources>
```

### 版本管理
```csharp
// 版本信息
public static class AppInfo
{
    public static string Version => 
        Package.Current.Id.Version.Major + "." +
        Package.Current.Id.Version.Minor + "." +
        Package.Current.Id.Version.Build;
}
```

## 常见反模式

### 避免的做法
1. **过度自定义**：不要过度修改原生组件外观
2. **硬编码**：不要硬编码尺寸、颜色、字符串
3. **内存泄漏**：不要忘记取消事件订阅
4. **UI 阻塞**：不要在 UI 线程执行耗时操作
5. **过度嵌套**：不要创建过深的布局嵌套

### 推荐的做法
1. **使用主题资源**：保持主题一致性
2. **数据绑定**：使用 MVVM 模式分离关注点
3. **异步编程**：使用 async/await 处理异步操作
4. **错误处理**：提供友好的错误信息
5. **无障碍支持**：确保所有用户都能使用