# WinUI Gallery Reference Workflow

**Goal:** Provide comprehensive WinUI 3 component references from the official WinUI Gallery to help developers implement controls correctly.

## WORKFLOW OVERVIEW

This skill provides a streamlined workflow for fetching and presenting WinUI 3 component references:

1. **Parse Request** - Understand what component or functionality is needed
2. **Identify Component** - Map the request to specific WinUI controls
3. **Fetch Reference** - Get code examples and documentation from WinUI Gallery
4. **Present Reference** - Format and deliver the reference information

## STEP 1: PARSE REQUEST

### Input Analysis
- **Component Name**: Direct component name (e.g., "NavigationView", "Button")
- **Functionality Description**: What the user wants to achieve (e.g., "navigation menu", "data entry form")
- **Context**: Where the component will be used (e.g., "settings page", "main window")

### Request Types
1. **Direct Component Request**: "How to use NavigationView?"
2. **Functionality Request**: "How to create a navigation menu?"
3. **Implementation Request**: "I need to implement a settings page"

## STEP 2: IDENTIFY COMPONENT

### Component Mapping
Use the component mapping in `./component-mapping.md` to:
- Map functionality descriptions to specific components
- Identify related components that might be needed
- Suggest component combinations for complex scenarios

### Smart Matching Rules
1. **Exact Match**: Direct component name → Use that component
2. **Keyword Match**: Functionality keywords → Find matching components
3. **Context Match**: Usage context → Suggest appropriate components
4. **Fallback**: If no match → Search WinUI Gallery for relevant examples

## STEP 3: FETCH REFERENCE

### GitHub API Strategy
Use `webfetch` to access WinUI Gallery content:

1. **Get Component Page**
   ```
   URL: https://github.com/microsoft/WinUI-Gallery/tree/main/WinUIGallery/Samples/ControlPages/{ComponentName}Page.xaml
   ```

2. **Get Code Examples**
   - XAML: `{ComponentName}Page.xaml`
   - C#: `{ComponentName}Page.xaml.cs`
   - Styles: Check `Styles/` directory if applicable

3. **Get Documentation**
   - Microsoft Learn: `https://learn.microsoft.com/en-us/windows/winui/api/{namespace}`
   - Component overview: Check README or documentation files

### Content Extraction
From the fetched content, extract:
- **XAML Examples**: UI markup and structure
- **C# Code**: Code-behind and logic
- **Styles**: Custom styling examples
- **Best Practices**: Implementation patterns

## STEP 4: PRESENT REFERENCE

### Reference Format
Present the reference in this structured format:

```markdown
# {ComponentName} 参考

## 概述
- 功能描述
- 适用场景
- 基本用法

## 基本用法
### XAML 示例
```xml
<!-- 基本XAML代码 -->
```

### C# 代码
```csharp
// 基本C#代码
```

## 高级用法
### 自定义样式
```xml
<!-- 自定义样式示例 -->
```

### 数据绑定
```xml
<!-- 数据绑定示例 -->
```

## 最佳实践
- 使用建议
- 性能优化
- 无障碍支持
- 常见错误避免

## 相关组件
- 相关组件列表和组合使用建议

## 参考链接
- [Microsoft Learn 文档]
- [WinUI Gallery 示例]
```

## USAGE EXAMPLES

### Example 1: Direct Component Request
**User**: "How to use NavigationView?"
**AI**: Fetches NavigationView reference from WinUI Gallery

### Example 2: Functionality Request
**User**: "How to create a navigation menu?"
**AI**: 
1. Maps "navigation menu" to NavigationView component
2. Fetches NavigationView reference
3. May also suggest BreadcrumbBar for hierarchical navigation

### Example 3: Implementation Request
**User**: "I need to implement a settings page"
**AI**:
1. Maps "settings page" to SettingsCard, Expander, etc.
2. Fetches references for multiple components
3. Provides guidance on component combination

## ERROR HANDLING

### Network Issues
- **Problem**: Cannot access GitHub
- **Solution**: Provide cached references if available, or suggest offline resources

### Component Not Found
- **Problem**: Component doesn't exist in WinUI Gallery
- **Solution**: Search for similar components or suggest alternative approaches

### Ambiguous Request
- **Problem**: Request could map to multiple components
- **Solution**: Ask for clarification or provide multiple references

## INTEGRATION POINTS

### Standalone Usage
- Direct invocation by user
- Manual reference lookup

### Development Workflow Integration
- Can be called by other skills (e.g., bmad-quick-dev)
- Automatic reference fetching during implementation

## QUALITY ASSURANCE

### Reference Accuracy
- Always fetch from official WinUI Gallery repository
- Verify code examples compile and run
- Include version information when relevant

### Completeness
- Provide both XAML and C# examples
- Include styling and customization options
- Link to official documentation

### Relevance
- Match references to user's specific use case
- Provide context-appropriate examples
- Suggest related components when helpful