# JsonParser

## 概述

`JsonParser` 是一个用于解析和序列化 JSON 数据的工具类，支持从指定 URL 异步解析在线 JSON 文件，并将 `PatchDetails` 对象序列化为格式化的 JSON 字符串。

## 类定义

```csharp
public partial class JsonParser(string url)
```

## 构造函数

### `JsonParser(string url)`

#### 描述

初始化 `JsonParser` 实例，指定待解析的 JSON 文件 URL。

#### 参数

| 参数名 | 类型     | 描述                | 必填 | 默认值 |
| ------ | -------- | ------------------- | ---- | ------ |
| `url`  | `string` | JSON 文件的网络地址 | ✅   | -      |

#### 示例

```csharp
// 初始化 JsonParser 实例，指定远程 JSON 文件 URL
var parser = new JsonParser("https://example.com/wechat_patch.json");
```

## 公有方法

### `ParseAsync()`

#### 描述

异步解析远程 JSON 文件，反序列化为 `PatchDetails` 对象。
解析过程中会自动处理网络异常、JSON 格式错误等问题，失败时返回 `null`。

#### 返回值

| 类型                  | 描述                                              |
| --------------------- | ------------------------------------------------- |
| `Task<PatchDetails?>` | 解析后的 `PatchDetails` 对象，解析失败时为 `null` |

#### 示例

```csharp
var parser = new JsonParser("https://example.com/wechat_patch.json");
var patchDetails = await parser.ParseAsync();

if (patchDetails != null)
{
    Console.WriteLine($"解析成功：微信版本 {patchDetails.WeChat.Version.Keys.First()}");
    // 处理解析后的业务逻辑
}
else
{
    Console.WriteLine("JSON 解析失败，请检查 URL 或文件格式");
}
```

### `SerializeJson(string version, string offset, string oldValue, string newValue)`

#### 描述

将版本补丁信息序列化为格式化的 JSON 字符串。
自动创建 `PatchDetails` 对象并按指定格式（缩进美化）输出。

#### 参数

| 参数名     | 类型     | 描述                            | 必填 | 默认值 |
| ---------- | -------- | ------------------------------- | ---- | ------ |
| `version`  | `string` | 微信版本号（如 `"8.0.37"`）     | ✅   | -      |
| `offset`   | `string` | 特征码偏移量（如 `"0x123456"`） | ✅   | -      |
| `oldValue` | `string` | 旧特征值                        | ✅   | -      |
| `newValue` | `string` | 新特征值                        | ✅   | -      |

#### 返回值

| 类型     | 描述                                 |
| -------- | ------------------------------------ |
| `string` | 序列化后的 JSON 字符串（带缩进格式） |

#### 示例

```csharp
var parser = new JsonParser("https://example.com/wechat_patch.json");
var json = parser.SerializeJson(
    version: "8.0.37",
    offset: "0x456789",
    oldValue: "old_feature",
    newValue: "new_feature"
);

Console.WriteLine("生成的 JSON 内容：");
Console.WriteLine(json);
```

#### 输出结果

```json
{
  "weChat": {
    "version": {
      "8.0.37": {
        "offset": "0x456789",
        "oldValue": "old_feature",
        "newValue": "new_feature"
      }
    }
  }
}
```

## 私有方法（内部使用）

### `FetchJsonAsync(string jsonUrl)`

#### 描述

从指定 URL 异步获取原始 JSON 字符串。
包含 URL 有效性校验、网络请求异常处理和超时控制。

#### 参数

| 参数名    | 类型     | 描述                     | 必填 | 默认值 |
| --------- | -------- | ------------------------ | ---- | ------ |
| `jsonUrl` | `string` | 目标 JSON 文件的完整 URL | ✅   | -      |

#### 返回值

| 类型           | 描述                   |
| -------------- | ---------------------- |
| `Task<string>` | 成功获取的 JSON 字符串 |

#### 异常

| 异常类型               | 触发条件                                   |
| ---------------------- | ------------------------------------------ |
| `ArgumentException`    | `jsonUrl` 为空或无效                       |
| `HttpRequestException` | 网络请求失败（如 4xx/5xx 状态码）          |
| `TimeoutException`     | 请求超时（默认由 HttpClient 全局配置控制） |

#### 示例（演示异常处理逻辑）

```csharp
try
{
    // 注意：私有方法通常不建议直接调用，此处仅演示内部逻辑
    var json = await JsonParser.FetchJsonAsync("https://invalid-url.com/data.json");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"错误：URL 无效 - {ex.Message}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"错误：网络请求失败 - {ex.StatusCode}，详情：{ex.Message}");
}
catch (TimeoutException ex)
{
    Console.WriteLine($"错误：请求超时 - {ex.Message}");
}
```

## 最佳实践

1. **异常处理**：在调用 `ParseAsync()` 时建议添加异常捕获，处理网络或格式错误。
2. **日志集成**：类内部已集成 Serilog 日志（`Log.Information`/`Log.Error`），可通过配置 Serilog 输出解析过程详情。
3. **性能优化**：重复调用时建议复用 `JsonParser` 实例，避免频繁创建 `HttpClient` 对象。
