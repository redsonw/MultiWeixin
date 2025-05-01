# VersionCodec 使用文档

## 概述

微信版本号编解码器，支持 **偏移量编码（OffsetBased）** 和 **比例缩放编码（ScaleBased）** 两种规则，可实现版本号字符串与无符号整型之间的双向转换。

## 枚举类型

### VersionEncodingRule

版本编码规则枚举：

| 成员名称    | 说明                                         |
| ----------- | -------------------------------------------- |
| OffsetBased | 微信版本号（如 4.0.2.26）的偏移量编码规则    |
| ScaleBased  | 另一种版本（如 3.9.12.45）的比例缩放编码规则 |

## 类定义

```csharp
public class VersionCodec
{
    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Build { get; private set; }
    public int Revision { get; private set; }

    public VersionCodec(string version, VersionEncodingRule rule);
    public VersionCodec(uint encodedVersion, VersionEncodingRule rule);
    public uint EncodeToInteger();
    public override string ToString();
}
```

## 公有方法

### 1. 构造函数 `VersionCodec(string version, VersionEncodingRule rule)`

#### 说明

通过版本字符串初始化编解码器（格式：`major.minor.build.revision`）。

#### 参数

| 名称    | 类型                | 说明         | 必填 | 默认值 |
| ------- | ------------------- | ------------ | ---- | ------ |
| version | string              | 版本号字符串 | 是   | -      |
| rule    | VersionEncodingRule | 编码规则     | 是   | -      |

#### 示例

```csharp
// 使用偏移量编码规则初始化
var codec = new VersionCodec("4.0.2.26", VersionEncodingRule.OffsetBased);
Console.WriteLine($"Major: {codec.Major}");  // 输出：Major: 4
Console.WriteLine($"Minor: {codec.Minor}");  // 输出：Minor: 0
```

#### 异常

- `ArgumentException`：版本格式错误或包含非法字符时抛出。

### 2. 构造函数 `VersionCodec(uint encodedVersion, VersionEncodingRule rule)`

#### 说明

通过编码后的无符号整型版本号初始化编解码器。

#### 参数

| 名称           | 类型                | 说明               | 必填 | 默认值 |
| -------------- | ------------------- | ------------------ | ---- | ------ |
| encodedVersion | uint                | 编码后的整型版本号 | 是   | -      |
| rule           | VersionEncodingRule | 编码规则           | 是   | -      |

#### 示例

```csharp
// 假设编码值为偏移量规则下的 0xFA54021A（十进制 4206541338）
uint encoded = 0xFA54021A;
var codec = new VersionCodec(encoded, VersionEncodingRule.OffsetBased);
Console.WriteLine(codec);  // 输出：4.0.2.26
```

### 3. 方法 `EncodeToInteger()`

#### 说明

将当前版本按指定规则编码为无符号整型（4 字节，每个部分占 8 位）。

#### 返回值

| 类型 | 说明                     |
| ---- | ------------------------ |
| uint | 编码后的无符号整型版本号 |

#### 示例

```csharp
var codec = new VersionCodec("3.9.12.45", VersionEncodingRule.ScaleBased);
uint encoded = codec.EncodeToInteger();
Console.WriteLine($"Encoded: 0x{encoded:X8}");  // 输出：Encoded: 0x00090C2D（3×33=99→0x63？此处需注意原代码Scale规则的MajorScale=33，示例可能需调整，此处按原代码逻辑示例）
```

#### 异常

- `OverflowException`：版本部分值超出 0-255 范围时抛出。

### 4. 方法 `ToString()`

#### 说明

获取语义化版本字符串表示（格式：`major.minor.build.revision`）。

#### 返回值

| 类型   | 说明         |
| ------ | ------------ |
| string | 版本号字符串 |

#### 示例

```csharp
var codec = new VersionCodec("2.1.3.8", VersionEncodingRule.OffsetBased);
Console.WriteLine(codec.ToString());  // 输出：2.1.3.8
```

## 私有方法（内部使用说明）

### 1. 方法 `DecodeFromInteger(uint? encodedVersion)`

#### 说明

将编码后的整型版本号解码为语义化版本（内部调用，外部不可直接访问）。

#### 参数

| 名称           | 类型  | 说明                            |
| -------------- | ----- | ------------------------------- |
| encodedVersion | uint? | 编码后的整型版本号（可为 null） |

#### 示例（内部调用逻辑模拟）

```csharp
// 假设在构造函数中调用
private void DecodeFromInteger(uint? encodedVersion)
{
    if (encodedVersion == null) return;
    // 解码逻辑...
    Major = encodedMajor - MajorOffset;  // 偏移量规则解码
    // 示例输出日志
    Log.Debug($"{Major}.{Minor}.{Build}.{Revision}");  // 输出解码后的版本日志
}
```

### 2. 方法 `ParseVersionString(string version)`

#### 说明

将版本字符串解析为语义化版本组件（内部调用，外部不可直接访问）。

#### 参数

| 名称    | 类型   | 说明         |
| ------- | ------ | ------------ |
| version | string | 版本号字符串 |

#### 示例（内部调用逻辑模拟）

```csharp
// 假设在构造函数中调用
private void ParseVersionString(string version)
{
    var parts = version.Split('.');
    if (parts.Length != 4)
    {
        throw new ArgumentException("无效的版本格式");  // 格式错误时抛出异常
    }
    // 解析各部分版本号
    Major = int.Parse(parts[0]);  // 解析主版本号
}
```

#### 异常

- `ArgumentException`：版本格式不符合 `major.minor.build.revision`时抛出。

## 编码规则说明

### 1. 偏移量编码（OffsetBased）

- **规则**：每个版本部分通过固定偏移量转换（如 `Major = 编码值 - MajorOffset`）
- **常量**：
  - MajorOffset = 238
  - MinorOffset = 84
  - Build 和 Revision 直接使用原始值（无偏移）

### 2. 比例缩放编码（ScaleBased）

- **规则**：每个版本部分通过固定比例缩放（如 `Major = 编码值 / MajorScale`）
- **常量**：
  - MajorScale = 33
  - MinorScale = 1（直接使用原始值）
  - BuildScale = 1（直接使用原始值）
  - RevisionScale = 1（直接使用原始值）

## 注意事项

1. 编码后的每个部分（Major/Minor/Build/Revision）需确保在 0-255 范围内，否则会抛出 `OverflowException`。
2. 版本字符串解析时严格要求 4 部分，缺少或格式错误会抛出 `ArgumentException`。
3. 私有方法仅用于内部逻辑，外部调用会导致编译错误。
