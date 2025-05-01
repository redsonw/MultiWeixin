# WeChatInfoMonitor

## 概述

`WeChatInfoMonitor` 类用于从注册表中读取微信相关信息，支持获取微信（WeChat）和微信旧版（Weixin）的版本、安装路径及文件保存路径（仅 WeChat）。通过解析注册表中的不同数据类型（字符串、DWORD 值、二进制 DWORD），提供统一的信息获取接口。

## 公有方法

### GetWechatInfo

获取微信相关配置信息，封装为 `WeChatConfig` 对象。

#### 语法

```csharp
public static WeChatConfig GetWechatInfo()
```

#### 返回值

| 类型           | 说明                                                                                                                                                                                                                                           |
| -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `WeChatConfig` | 微信配置对象，包含以下属性：`<br>`- `WeChatVersion`：微信版本号 `<br>`- `WeChatInstallPath`：微信安装路径 `<br>`- `WeChatFileSavePath`：微信文件保存路径 `<br>`- `WeixinVersion`：微信旧版版本号 `<br>`- `WeixinInstallPath`：微信旧版安装路径 |

#### 示例

```csharp
// 调用公有方法获取微信配置
WeChatConfig config = WeChatInfoMonitor.GetWechatInfo();

// 输出结果
Console.WriteLine($"微信版本：{config.WeChatVersion}");
Console.WriteLine($"微信安装路径：{config.WeChatInstallPath}");
Console.WriteLine($"微信文件保存路径：{config.WeChatFileSavePath}");
Console.WriteLine($"微信旧版版本：{config.WeixinVersion}");
Console.WriteLine($"微信旧版安装路径：{config.WeixinInstallPath}");
```

## 私有方法

### GetValue（内部使用）

从注册表指定子键中获取指定值名称的字符串表示，支持处理 DWORD 值（整数/二进制）和字符串类型。

#### 语法

```csharp
private static string GetValue(string subKey, string valueName)
```

#### 参数

| 名称        | 类型   | 说明                                                          |
| ----------- | ------ | ------------------------------------------------------------- |
| `subKey`    | string | 注册表子键（如 "WeChat" 或 "Weixin"）                         |
| `valueName` | string | 值名称（使用 `ValueName` 内部类常量，如 `ValueName.Version`） |

#### 返回值

| 类型   | 说明                                         |
| ------ | -------------------------------------------- |
| string | 注册表值的字符串表示，解析失败时返回空字符串 |

#### 示例

```csharp
// 在类内部调用私有方法获取微信版本号（示例为类内实现逻辑）
public static WeChatConfig GetWechatInfo()
{
    return new WeChatConfig()
    {
        // 调用私有方法获取不同值
        WeChatVersion = GetValue(WeChatSubKey, ValueName.Version),
        WeChatInstallPath = GetValue(WeChatSubKey, ValueName.InstallPath),
        // ... 其他配置项同理
    };
}

// 处理特殊类型示例：当注册表值为 DWORD 类型（如二进制格式）时
// 方法内部通过 `switch` 语句自动转换为无符号整数字符串
byte[] dwordBytes = { 0x01, 0x00, 0x00, 0x00 }; // 对应十进制 1
string result = GetValue("任意子键", "任意值名", dwordBytes); // 输出 "1"
```

## 内部类：ValueName

定义注册表值名称常量，避免硬编码。

### 成员

| 名称           | 说明                            |
| -------------- | ------------------------------- |
| `Version`      | 版本号值名称                    |
| `InstallPath`  | 安装路径值名称                  |
| `FileSavePath` | 文件保存路径值名称（仅 WeChat） |
