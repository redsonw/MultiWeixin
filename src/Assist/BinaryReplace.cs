using System.Collections.Concurrent;
using System.IO;
using MultiWeixin.Models;

namespace MultiWeixin.Assist;

/// <summary>
/// 二进制文件模式替换处理器
/// <para>提供高效的二进制模式搜索和替换功能，支持并行处理和异步操作</para>
/// </summary>
/// <param name="options">包含文件路径、搜索模式、替换模式的配置参数</param>
public class BinaryReplace(Options options)
{
    private const int ParallelMultiplier = 2; // 并行计算系数
    private readonly List<ReplaceSegment> _replaceSegments = PreprocessReplaceBytes(options.ReplaceByte);
    private readonly int _maxReplaceLength = CalculateMaxReplaceLength(options.ReplaceByte);

    /// <summary>
    /// 配置文件参数
    /// </summary>
    public Options Options { get; } = options ?? throw new ArgumentNullException(nameof(options));

    #region Public Methods
    /// <summary>
    /// 同步搜索匹配的字节模式
    /// </summary>
    /// <returns>匹配位置的偏移量集合（升序排列）</returns>
    public List<long> Search()
    {
        var buffer = ReadFileBuffer();
        return FindPatternMatches(buffer);
    }

    /// <summary>
    /// 搜索指定字节模式并返回目标位置的偏移量
    /// </summary>
    /// <param name="targetIndex">目标字节在模式中的索引位置</param>
    /// <returns>所有匹配项中目标字节的偏移地址列表</returns>
    public List<long> SearchWithTargetIndex(int targetIndex)
    {
        var buffer = ReadFileBuffer();
        return FindPatternMatchesWithTarget(buffer, targetIndex);
    }

    /// <summary>
    /// 异步搜索指定字节模式并返回目标位置的偏移量
    /// </summary>
    /// <param name="targetIndex">目标字节在模式中的索引位置</param>
    /// <returns>包含目标字节偏移地址的任务</returns>
    public async Task<List<long>> SearchWithTargetIndexAsync(int targetIndex)
    {
        var buffer = await ReadFileBufferAsync();
        return FindPatternMatchesWithTarget(buffer, targetIndex);
    }

    /// <summary>
    /// 查找匹配模式并提取目标位置偏移量
    /// </summary>
    private List<long> FindPatternMatchesWithTarget(byte[] buffer, int targetIndex)
    {
        var searchPattern = Options.SearchByte.ToArray();
        ValidateTargetIndex(searchPattern, targetIndex);

        var matches = new ConcurrentBag<long>();
        long maxIndex = buffer.Length - searchPattern.Length;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * ParallelMultiplier
        };

        Parallel.For(0, (int)maxIndex + 1, parallelOptions, i =>
        {
            if (IsPatternMatch(buffer, i, searchPattern))
            {
                // 计算目标字节的绝对偏移量
                var targetOffset = CalculateTargetOffset(searchPattern, i, targetIndex);
                if (targetOffset != -1)
                {
                    matches.Add(targetOffset);
                }
            }
        });

        return matches.Order().ToList();
    }

    /// <summary>
    /// 验证目标索引有效性
    /// </summary>
    private static void ValidateTargetIndex(byte?[] pattern, int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= pattern.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(targetIndex),
                $"目标索引 {targetIndex} 超出模式范围 (0-{pattern.Length - 1})");
        }

        if (!pattern[targetIndex].HasValue)
        {
            throw new ArgumentException("目标位置不能是通配符", nameof(targetIndex));
        }
    }

    /// <summary>
    /// 计算实际目标偏移量（跳过通配符）
    /// </summary>
    private static long CalculateTargetOffset(byte?[] pattern, long baseOffset, int targetIndex)
    {
        // 直接计算绝对偏移量
        return baseOffset + targetIndex;
    }

    /// <summary>
    /// 异步搜索匹配的字节模式
    /// </summary>
    /// <returns>包含匹配位置偏移量的任务</returns>
    public async Task<List<long>> SearchAsync()
    {
        var buffer = await ReadFileBufferAsync();
        return FindPatternMatches(buffer);
    }

    /// <summary>
    /// 同步执行批量替换操作
    /// </summary>
    /// <param name="offsets">需要替换的偏移量集合</param>
    /// <exception cref="FileNotFoundException">目标文件不存在时抛出</exception>
    public void Replace(IEnumerable<long> offsets)
    {
        ValidateFileExists();
        if (Options.FilePath == null) return;

        using var fs = new FileStream(Options.FilePath, FileMode.Open, FileAccess.ReadWrite);
        var orderedOffsets = PrepareOffsets(offsets);

        foreach (var offset in orderedOffsets)
        {
            ValidateReplaceOffset(fs, offset);
            ReplacePatternAtOffset(fs, offset);
        }
        fs.Flush();
    }

    /// <summary>
    /// 异步执行批量替换操作
    /// </summary>
    /// <param name="offsets">需要替换的偏移量集合</param>
    /// <returns>异步操作任务</returns>
    public async Task ReplaceAsync(IEnumerable<long> offsets)
    {
        ValidateFileExists();
        if (Options.FilePath == null) return;

        using var fs = new FileStream(Options.FilePath, FileMode.Open, FileAccess.ReadWrite);
        foreach (var offset in PrepareOffsets(offsets))
        {
            ValidateReplaceOffset(fs, offset);
            await ReplacePatternAtOffsetAsync(fs, offset);
        }
    }
    #endregion

    #region Core Logic
    /// <summary>
    /// 并行模式匹配算法（使用内存映射优化）
    /// </summary>
    private List<long> FindPatternMatches(byte[] buffer)
    {
        var searchPattern = Options.SearchByte.ToArray();
        var matches = new ConcurrentBag<long>();
        var maxIndex = buffer.Length - searchPattern.Length;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * ParallelMultiplier
        };

        Parallel.For(0, (int)maxIndex + 1, parallelOptions, i =>
        {
            if (IsPatternMatch(buffer, i, searchPattern))
            {
                matches.Add(i);
            }
        });

        return matches.Order().ToList();
    }

    /// <summary>
    /// 字节模式匹配验证（支持通配符）
    /// </summary>
    private static bool IsPatternMatch(byte[] buffer, long startIndex, byte?[] searchPattern)
    {
        if (startIndex + searchPattern.Length > buffer.Length) return false; // 添加长度校验

        for (int i = 0; i < searchPattern.Length; i++)
        {
            var currentByte = searchPattern[i]; // 提取当前元素

            if (currentByte.HasValue && buffer[startIndex + i] != currentByte.Value)
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 预处理替换偏移量集合
    /// </summary>
    private static List<long> PrepareOffsets(IEnumerable<long> offsets)
    {
        return offsets.Distinct()
                      .Order()
                      .ToList();
    }

    /// <summary>
    /// 计算最大替换长度（用于边界校验）
    /// </summary>
    private static int CalculateMaxReplaceLength(IEnumerable<byte?> replaceBytes)
    {
        // 处理空输入或全通配符的情况
        var validLengths = replaceBytes?
            .Select((b, i) => b.HasValue ? i + 1 : 0)
            .Where(v => v > 0)  // 过滤掉无效值
            .DefaultIfEmpty(0);  // 确保至少有一个元素

        return validLengths?.Max() ?? 0;
    }

    /// <summary>
    /// 读取完整文件缓冲区（同步）
    /// </summary>
    private byte[] ReadFileBuffer()
    {
        ValidateFileExists();
        if (Options.FilePath == null) return [];

        using var fs = new FileStream(Options.FilePath, FileMode.Open, FileAccess.Read);
        var buffer = new byte[fs.Length];
        fs.ReadExactly(buffer);
        return buffer;
    }

    /// <summary>
    /// 异步读取完整文件缓冲区
    /// </summary>
    private async Task<byte[]> ReadFileBufferAsync()
    {
        ValidateFileExists();
        if (Options.FilePath == null) return [];

        using var fs = new FileStream(Options.FilePath, FileMode.Open, FileAccess.Read);
        var buffer = new byte[fs.Length];
        await fs.ReadExactlyAsync(buffer);
        return buffer;
    }
    #endregion

    #region Validation
    /// <summary>
    /// 验证文件存在性
    /// </summary>
    private void ValidateFileExists()
    {
        if (!File.Exists(Options.FilePath))
        {
            throw new FileNotFoundException("目标文件未找到", Options.FilePath);
        }
    }

    /// <summary>
    /// 验证替换偏移量的有效性
    /// </summary>
    private void ValidateReplaceOffset(FileStream fs, long offset)
    {
        if (offset < 0 ||
            offset + _maxReplaceLength > fs.Length ||
            offset + Options.SearchByte.Count() > fs.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset),
                $"无效偏移量：{offset} (文件长度：{fs.Length})");
        }
    }
    #endregion

    #region Replace Operations
    /// <summary>
    /// 在指定偏移量执行同步替换
    /// </summary>
    private void ReplacePatternAtOffset(FileStream fs, long offset)
    {
        foreach (var segment in _replaceSegments)
        {
            fs.Seek(offset + segment.OffsetInPattern, SeekOrigin.Begin);
            fs.Write(segment.Bytes, 0, segment.Bytes.Length);
        }
    }

    /// <summary>
    /// 在指定偏移量执行异步替换
    /// </summary>
    private async Task ReplacePatternAtOffsetAsync(FileStream fs, long offset)
    {
        foreach (var segment in _replaceSegments)
        {
            fs.Seek(offset + segment.OffsetInPattern, SeekOrigin.Begin);
            await fs.WriteAsync(segment.Bytes.AsMemory(0, segment.Bytes.Length));
        }
    }

    /// <summary>
    /// 预处理替换字节为连续段
    /// </summary>
    private static List<ReplaceSegment> PreprocessReplaceBytes(IEnumerable<byte?> replaceBytes)
    {
        var segments = new List<ReplaceSegment>();
        var current = new ReplaceBuilder();

        foreach (var (b, index) in replaceBytes.Select((b, i) => (b, i)))
        {
            if (b.HasValue)
            {
                current.Add(b.Value, index);
            }
            else if (current.IsBuilding)
            {
                segments.Add(current.Build());
                current.Reset();
            }
        }

        if (current.IsBuilding) segments.Add(current.Build());
        return segments;
    }

    /// <summary>
    /// 替换段构建辅助类
    /// </summary>
    private class ReplaceBuilder
    {
        private int _startIndex = -1;
        private readonly List<byte> _bytes = new();

        public bool IsBuilding => _startIndex != -1;

        public void Add(byte value, int index)
        {
            if (_startIndex == -1) _startIndex = index;
            _bytes.Add(value);
        }

        public ReplaceSegment Build()
        {
            var segment = new ReplaceSegment(_startIndex, _bytes.ToArray());
            Reset();
            return segment;
        }

        public void Reset()
        {
            _startIndex = -1;
            _bytes.Clear();
        }
    }

    private record ReplaceSegment(int OffsetInPattern, byte[] Bytes);
    #endregion
}