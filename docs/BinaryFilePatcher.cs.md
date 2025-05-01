# BinaryFilePatcher

## 概述

提供二进制文件字节替换功能，支持按内存地址精准替换和按特征模式批量替换（含通配符匹配），内置文件备份、错误处理和性能优化机制。

## 安装

```
// 直接引用该类文件或通过项目依赖管理

using MultiWeixin.Assist;
```

## 公有方法

### 1. `BinaryFilePatcher(string targetFilePath)`

#### 功能

初始化二进制文件修补器，验证目标文件路径有效性并生成备份文件路径。

#### 参数

| 名称           | 类型   | 说明             | 必填 | 默认值 |
| -------------- | ------ | ---------------- | ---- | ------ |
| targetFilePath | string | 目标文件完整路径 | 是   | -      |

#### 示例

```
var patcher = new BinaryFilePatcher("C:\\\target\\\file.exe");
```

### 2. `bool ReplaceByte(string hexAddress, string expectedValue, string newValue)`

#### 功能

按十六进制内存地址替换单个字节，需验证原字节值是否符合预期。

#### 参数

| 名称          | 类型   | 说明                               | 必填 | 默认值 |
| ------------- | ------ | ---------------------------------- | ---- | ------ |
| hexAddress    | string | 十六进制内存地址（支持 `0x` 前缀） | 是   | -      |
| expectedValue | string | 预期的原字节值（十六进制）         | 是   | -      |
| newValue      | string | 新的字节值（十六进制）             | 是   | -      |

#### 返回值

| 类型 | 说明                           |
| ---- | ------------------------------ |
| bool | 替换是否成功（`true`/`false`） |

#### 示例

```
var patcher = new BinaryFilePatcher("C:\\\target\\\file.dat");

bool success = patcher.ReplaceByte("0x1234", "0xAB", "0xCD");

if (success)

{

    Console.WriteLine("字节替换成功");

}

else

{

    Console.WriteLine("字节替换失败");

}
```

### 3. `bool ReplacePattern(byte[] searchPattern, byte[] replacementPattern, bool allowWildcard = false, int maxReplaceCount = 1)`

#### 功能

按字节模式查找并替换，支持通配符 `0xFF` 和最大替换次数限制。

#### 参数

| 名称               | 类型    | 说明                                            | 必填 | 默认值  |
| ------------------ | ------- | ----------------------------------------------- | ---- | ------- |
| searchPattern      | byte\[] | 待查找的字节模式（支持 `0xFF` 通配符）          | 是   | -       |
| replacementPattern | byte\[] | 替换的字节模式（长度需与 `searchPattern` 一致） | 是   | -       |
| allowWildcard      | bool    | 是否启用通配符匹配                              | 否   | `false` |
| maxReplaceCount    | int     | 最大替换次数                                    | 否   | `1`     |

#### 返回值

| 类型 | 说明                   |
| ---- | ---------------------- |
| bool | 是否执行了至少一次替换 |

#### 示例

```
var patcher = new BinaryFilePatcher("C:\\\target\\\resource.bin");

byte[] searchPattern = { 0x48, 0x65, 0xFF, 0x6C, 0x6C, 0x6F }; // 包含通配符

byte[] replacementPattern = { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x21 }; // 替换为 "Hello!"

bool modified = patcher.ReplacePattern(searchPattern, replacementPattern, allowWildcard: true);

if (modified)

{

   Console.WriteLine("模式替换成功");

}
```

## 私有方法示例

### 1. `List<int> FindPatternMatches(byte[] source, byte[] pattern, bool allowWildcard)`

#### 功能

内部使用 Boyer-Moore 算法查找所有匹配模式的偏移量（供 `ReplacePattern` 调用）。

#### 示例

```
// 模拟文件数据和搜索模式

byte[] sourceData = { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 };

byte[] pattern = { 0x6C, 0xFF, 0x6F }; // 匹配 "llo" 或 "lo"（通配符启用时）

var matches = BinaryFilePatcher.FindPatternMatches(sourceData, pattern, allowWildcard: true);

// 输出匹配结果：\[2, 9]（假设偏移量）
```

### 2. `int[] BuildBadCharTable(byte[] pattern, bool allowWildcard)`

#### 功能

构建坏字符跳转表优化模式匹配效率（供 `FindPatternMatches` 调用）。

#### 示例

```
byte[] pattern = { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

int[] badCharTable = BinaryFilePatcher.BuildBadCharTable(pattern, allowWildcard: false);

// 表中存储每个字符在模式中的最后出现位置（如 0x48 对应索引 0，0x65 对应索引 1 等）
```

### 3. `bool IsByteMatch(byte source, byte pattern, bool allowWildcard)`

#### 功能

判断字节是否匹配（支持通配符逻辑）。

#### 示例

```
bool match1 = BinaryFilePatcher.IsByteMatch(0x65, 0xFF, allowWildcard: true); // true（通配符匹配）

bool match2 = BinaryFilePatcher.IsByteMatch(0x61, 0x62, allowWildcard: false); // false（值不匹配）
```

### 4. `void CreateBackupOnce()`

#### 功能

线程安全地创建唯一备份文件（首次调用修改方法时自动执行）。

#### 示例

```
var patcher = new BinaryFilePatcher("C:\\\target\\\file.dll");

// 调用 ReplaceByte 或 ReplacePattern 时会自动触发备份

patcher.ReplaceByte("0x100", "0x00", "0x01");

// 生成备份文件：file.dll.backup.dll
```

### 5. `void ValidateAddress(long fileLength, long address)`

#### 功能

验证内存地址是否在文件范围内，超出范围时抛出异常。

#### 示例

```
long fileLength = 1024;

long validAddress = 512;

long invalidAddress = 2048;

BinaryFilePatcher.ValidateAddress(fileLength, validAddress); // 无异常

// BinaryFilePatcher.ValidateAddress(fileLength, invalidAddress); // 抛出 ArgumentOutOfRangeException
```

### 6. `void ValidatePatterns(byte[] search, byte[] replace)`

#### 功能

验证搜索模式和替换模式的合法性（非空且长度一致）。

#### 示例

```
byte[] validSearch = { 0x01, 0x02 };

byte[] validReplace = { 0x03, 0x04 };

BinaryFilePatcher.ValidatePatterns(validSearch, validReplace); // 无异常

byte[] invalidSearch = null;

// BinaryFilePatcher.ValidatePatterns(invalidSearch, validReplace); // 抛出 ArgumentException
```

### 7. `bool TryParseHexWithPrefix(string input, out long result)`

#### 功能

解析带 `0x` 前缀的十六进制地址字符串。

#### 示例

```
string addressInput = "0x1A2B";

long parsedAddress;

bool success = BinaryFilePatcher.TryParseHexWithPrefix(addressInput, out parsedAddress);

// success = true, parsedAddress = 0x1A2B（十进制 6707）
```

### 8. `bool TryParseHexByteWithPrefix(string input, out byte result)`

#### 功能

解析带 `0x` 前缀的十六进制字节字符串。

#### 示例

```
string byteInput = "0xFF";

byte parsedByte;

bool success = BinaryFilePatcher.TryParseHexByteWithPrefix(byteInput, out parsedByte);

// success = true, parsedByte = 0xFF（255）
```

### 9. `void HandleError(string message, Exception ex)`

#### 功能

统一处理异常并记录日志（根据异常类型输出不同提示）。

#### 示例

```
try

{

   // 模拟文件不存在异常

   var patcher = new BinaryFilePatcher("C:\\\nonexistent\\\file.exe");

}

catch (Exception ex)

{

   var patcher = new BinaryFilePatcher("C:\\\target\\\file.exe");

   patcher.HandleError("初始化失败", ex);

   // 日志输出："目标文件不存在，请检查路径：C:\nonexistent\file.exe"

}
```

## 注意事项

**备份机制**：首次调用修改方法（`ReplaceByte`/`ReplacePattern`）时自动创建备份文件，路径为 `原文件名.backup.扩展名`。

**通配符规则**：仅当 `allowWildcard` 为 `true` 时，`0xFF` 表示任意字节匹配。

**性能优化**：大文件处理使用内存映射文件（`MemoryMappedFile`）和 Boyer-Moore 算法提升效率。

**错误处理**：所有文件操作异常均通过 `Serilog` 记录，包含地址越界、权限不足、文件被占用等场景。
