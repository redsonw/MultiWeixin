# BinaryReplace.cs

## 概述

提供高效的二进制文件模式搜索与替换功能，支持并行搜索和异步操作，适用于大文件处理场景。

## 构造函数

### 定义

```csharp
public class BinaryReplace(Options options)
```

### 参数

| 参数名  | 类型      | 说明                                       | 必传 | 默认值 |
| ------- | --------- | ------------------------------------------ | ---- | ------ |
| options | `Options` | 配置参数，包含文件路径、搜索模式、替换模式 | 是   | -      |

### 示例代码

```csharp
var options = new Options
{
    FilePath = "target.bin",
    SearchByte = new List<byte?> { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, // "Hello"
    ReplaceByte = new List<byte?> { 0x48, null, 0x6C, 0x6C, 0x6F } // 通配符示例
};
var binaryReplace = new BinaryReplace(options);
```

## 公有方法

### 1. Search()

#### 功能

同步搜索匹配的字节模式，返回匹配位置的偏移量集合（升序排列）。

#### 返回值

`List<long>` - 匹配位置的偏移量集合

#### 示例代码

```csharp
var offsets = binaryReplace.Search();
Console.WriteLine($"Found {offsets.Count} matches.");
```

### 2. SearchWithTargetIndex(int targetIndex)

#### 功能

搜索指定字节模式并返回目标位置的偏移量（目标位置由索引指定）。

#### 参数

| 参数名      | 类型  | 说明                                       |
| ----------- | ----- | ------------------------------------------ |
| targetIndex | `int` | 目标字节在模式中的索引（需为非通配符位置） |

#### 返回值

`List<long>` - 目标字节的偏移地址列表

#### 示例代码

```csharp
var targetOffsets = binaryReplace.SearchWithTargetIndex(2); // 搜索模式中第3个字节的位置
targetOffsets.ForEach(offset => Console.WriteLine($"Target offset: {offset}"));
```

### 3. SearchAsync()

#### 功能

异步搜索匹配的字节模式，返回包含匹配位置偏移量的任务。

#### 返回值

`Task<List<long>>` - 异步任务

#### 示例代码

```csharp
var asyncOffsets = await binaryReplace.SearchAsync();
Console.WriteLine($"Async search found {asyncOffsets.Count} matches.");
```

### 4. SearchWithTargetIndexAsync(int targetIndex)

#### 功能

异步搜索指定字节模式并返回目标位置的偏移量。

#### 参数

| 参数名      | 类型  | 说明                                       |
| ----------- | ----- | ------------------------------------------ |
| targetIndex | `int` | 目标字节在模式中的索引（需为非通配符位置） |

#### 返回值

`Task<List<long>>` - 包含目标偏移量的异步任务

#### 示例代码

```csharp
var asyncTargetOffsets = await binaryReplace.SearchWithTargetIndexAsync(1);
asyncTargetOffsets.ForEach(offset => Console.WriteLine($"Async target offset: {offset}"));
```

### 5. Replace(IEnumerable\<long> offsets)

```html
<long></long>
```

#### 功能

同步执行批量替换操作，根据偏移量集合替换文件中的目标模式。

#### 参数

| 参数名  | 类型                | 说明                 |
| ------- | ------------------- | -------------------- |
| offsets | `IEnumerable<long>` | 需要替换的偏移量集合 |

#### 示例代码

```csharp
var replaceOffsets = new List<long> { 100, 200, 300 };
binaryReplace.Replace(replaceOffsets);
Console.WriteLine("Replacement completed synchronously.");
```

### 6. ReplaceAsync(IEnumerable\<long> offsets)

```html
<long></long>
```

#### 功能

异步执行批量替换操作，根据偏移量集合替换文件中的目标模式。

#### 参数

| 参数名  | 类型                | 说明                 |
| ------- | ------------------- | -------------------- |
| offsets | `IEnumerable<long>` | 需要替换的偏移量集合 |

#### 返回值

`Task` - 异步任务

#### 示例代码

```csharp
var asyncReplaceOffsets = new List<long> { 150, 250, 350 };
await binaryReplace.ReplaceAsync(asyncReplaceOffsets);
Console.WriteLine("Replacement completed asynchronously.");
```

## 私有方法（内部实现说明）

### 1. FindPatternMatches(byte\[] buffer)

#### 功能

并行模式匹配算法，在文件缓冲区中搜索匹配的字节模式（支持通配符）。

#### 示例（内部调用逻辑）

```csharp
// 公有方法 Search() 内部调用
private List<long> FindPatternMatches(byte[] buffer)
{
    // 并行搜索逻辑
    Parallel.For(0, maxIndex, i => { /* 匹配校验 */ });
    return matches.Order().ToList();
}
```

### 2. IsPatternMatch(byte\[] buffer, long startIndex, byte?\[] searchPattern)

#### 功能

验证字节模式是否匹配（支持通配符 `null` 表示任意字节）。

#### 示例（内部调用逻辑）

```csharp
// 在并行搜索中用于逐个字节校验
private static bool IsPatternMatch(byte[] buffer, long startIndex, byte?[] searchPattern)
{
    for (int i = 0; i < searchPattern.Length; i++)
    {
        if (searchPattern[i].HasValue && buffer[startIndex + i] != searchPattern[i].Value)
        {
            return false;
        }
    }
    return true;
}
```

### 3. PrepareOffsets(IEnumerable\<long> offsets)

```html
<long></long>
```

#### 功能

预处理偏移量集合，去重并按升序排列（避免替换顺序错误）。

#### 示例（内部调用逻辑）

```csharp
// 在替换操作前调用
private static List<long> PrepareOffsets(IEnumerable<long> offsets)
{
    return offsets.Distinct().Order().ToList(); // 去重并排序
}
```

### 4. PreprocessReplaceBytes(IEnumerable\<byte?> replaceBytes)

#### 功能

将替换字节预处理为连续段，优化文件写入性能（减少 Seek 操作次数）。

#### 示例（内部调用逻辑）

```csharp
// 在构造函数中初始化替换段
private List<ReplaceSegment> PreprocessReplaceBytes(IEnumerable<byte?> replaceBytes)
{
    // 将连续的有效字节合并为段
    foreach (var (b, index) in replaceBytes.Select((b, i) => (b, i)))
    {
        if (b.HasValue) { /* 合并到当前段 */ }
        else { /* 结束当前段 */ }
    }
    return segments;
}
```

## 配置参数 Options

```csharp
public class Options
{
    public string FilePath { get; set; } // 目标文件路径
    public List<byte?> SearchByte { get; set; } // 搜索模式（支持null通配符）
    public List<byte?> ReplaceByte { get; set; } // 替换模式（支持null通配符，仅有效字节会被写入）
}
```

## 注意事项

1. **通配符支持**：搜索模式和替换模式中的 `null` 表示任意字节（替换时忽略通配符位置）。
2. **性能优化**：并行搜索效率与处理器核心数相关（`ParallelMultiplier=2` 为默认系数）。
3. **偏移量校验**：替换前会自动校验偏移量有效性，避免越界操作。
