# MultiWeixin

## 简介

MultiWeixin 是一款用于实现微信多开功能的工具。该工具通过对微信二进制文件进行特定字节替换，解除微信的多开限制，让用户能够同时打开多个微信客户端。它支持从注册表获取微信版本信息和安装路径，并通过解析在线 JSON 文件获取对应版本的补丁信息。此外，工具还提供了日志记录功能，方便用户查看操作状态和错误信息。

## 功能特性

1. **微信多开支持**：解除微信多开限制，允许同时打开多个微信客户端。
2. **版本信息自动获取**：从注册表中自动获取微信的版本号和安装路径。
3. **补丁信息解析**：通过解析在线 JSON 文件，获取对应微信版本的补丁信息。
4. **二进制文件修补**：对微信二进制文件进行字节替换，实现多开功能。
5. **日志记录**：使用 `Serilog` 记录操作信息和错误信息，并在界面上实时显示。

## 项目结构

```plaintext
.gitattributes
.gitignore
LICENSE.txt
MultiWeixin.sln
README.md
docs/
  BinaryFilePatcher.cs.md
  BinaryReplace.cs.md
  Json.cs.md
  VersionCodec.cs.md
  WeChatInfoMonitor.cs.md
src/
  App.xaml
  App.xaml.cs
  AssemblyInfo.cs
  Assist/
  LogFormatter.cs
  MainWindow.xaml
  MainWindow.xaml.cs
  Models/
  MultiWeixin.csproj
  Rescource/
  Service/
  Themes/
  ViewModel/
```

### 主要文件和文件夹说明

- `.gitignore`：定义了 Git 版本控制中需要忽略的文件和文件夹。
- `src/`：包含项目的源代码文件。
  - `App.xaml` 和 `App.xaml.cs`：应用程序的入口点，负责初始化应用程序和依赖注入。
  - `MainWindow.xaml` 和 `MainWindow.xaml.cs`：主窗口的界面和逻辑代码。
  - `Assist/`：辅助类库，包含微信信息获取、JSON 解析、二进制文件修补等功能类。
  - `Themes/`：包含应用程序的样式和模板文件。
  - `ViewModel/`：包含视图模型类，负责处理业务逻辑和数据绑定。

## 安装与使用

### 安装

1. 克隆项目仓库到本地：

```bash
git clone https://github.com/your-repo/MultiWeixin.git
```

2. 使用 Visual Studio 打开 `MultiWeixin.sln` 解决方案。
3. 编译并运行项目。

### 使用

1. 启动应用程序后，界面会自动显示微信的版本信息。
2. 点击“解除多开”按钮，工具会自动从注册表获取微信安装路径，并根据版本信息从在线 JSON 文件中获取补丁信息。
3. 工具会对微信二进制文件进行字节替换，完成多开限制的解除。
4. 在操作过程中，日志信息会实时显示在界面上，方便用户查看操作状态和错误信息。

## 代码示例

### 微信版本信息获取

```csharp
public static WeChatConfig GetWechatInfo()
{
    return new WeChatConfig()
    {
        WeChatVersion = GetValue(WeChatSubKey, ValueName.Version),
        WeChatInstallPath = GetValue(WeChatSubKey, ValueName.InstallPath),
        WeChatFileSavePath = GetValue(WeChatSubKey, ValueName.FileSavePath),
        WeixinVersion = GetValue(WeixinSubKey, ValueName.Version),
        WeixinInstallPath = GetValue(WeixinSubKey, ValueName.InstallPath)
    };
}
```

### JSON 解析

```csharp
public async Task<PatchDetails?> ParseAsync()
{
    try
    {
        string jsonString = await FetchJsonAsync(_url);
        var patchDetails = JsonSerializer.Deserialize<PatchDetails>(jsonString, _jsonOptions) ?? throw new JsonException("反序列化时返回了 null 对象。");

        Log.Information("特征字库文件解析完成。");

        return patchDetails;
    }
    catch (HttpRequestException ex)
    {
        Log.Error($"网络连接错误: {ex.Message}");
    }
    catch (JsonException ex)
    {
        Log.Error($"JSON 解析错误: {ex.Message}");
    }
    catch (Exception ex)
    {
        Log.Error($"解析微信版本信息时发生错误: {ex.Message}");
    }

    return null;
}
```

### 二进制文件修补

```csharp
public bool ReplaceByte(string hexAddress, string expectedValue, string newValue)
{
    CreateBackupOnce();

    var parsedAddress = TryParseHexWithPrefix(hexAddress, out long address);
    var parsedExpected = TryParseHexByteWithPrefix(expectedValue, out byte expectedByte);
    var parsedNew = TryParseHexByteWithPrefix(newValue, out byte newByte);

    if (!parsedAddress)
    {
        return false;
    }

    if (!parsedExpected)
    {
        return false;
    }

    if (!parsedNew)
    {
        return false;
    }

    // 后续操作代码省略
}
```

## 注意事项

- 该工具仅用于学习和研究目的，请遵守相关法律法规和软件使用协议。
- 在使用工具前，请备份微信的二进制文件，以免出现不可恢复的错误。
- 由于微信版本更新可能会导致补丁信息失效，工具可能无法正常工作。请及时关注项目更新或自行更新补丁信息。

## 贡献

如果你对该项目感兴趣，可以通过以下方式进行贡献：

1. 提交 Bug 报告或功能请求。
2. 提交代码改进或新功能实现的 Pull Request。

## 许可证

本项目采用 [MIT 许可证](LICENSE.txt)。
