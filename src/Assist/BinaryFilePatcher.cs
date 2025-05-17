using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Serilog;

namespace MultiWeixin.Assist;

/// <summary>
/// 二进制文件修补器，提供基于内存地址和特征模式的字节替换功能
/// <para>支持通配符匹配和批量替换操作</para>
/// </summary>
public class BinaryFilePatcher
{
    private const byte WildcardByte = 0xFF; // 通配符定义
    private readonly string _targetFilePath;
    private readonly string _backupFilePath;
    private bool _backupCreated;
    private readonly object _backupLock = new object(); // 私有锁对象

    /// <summary>
    /// 初始化二进制文件修补器
    /// </summary>
    /// <param name="targetFilePath">目标文件完整路径</param>
    /// <exception cref="ArgumentException">路径无效时抛出</exception>
    public BinaryFilePatcher(string targetFilePath)
    {
        _targetFilePath = targetFilePath ?? throw new ArgumentNullException(nameof(targetFilePath));

        if (string.IsNullOrWhiteSpace(targetFilePath))
            throw new ArgumentException("文件路径不能为空或空白", nameof(targetFilePath));

        var directory = Path.GetDirectoryName(targetFilePath);
        var originalFileName = Path.GetFileNameWithoutExtension(targetFilePath);

        if (string.IsNullOrEmpty(originalFileName))
            throw new ArgumentException("文件名无效", nameof(targetFilePath));

        var extension = Path.GetExtension(targetFilePath);

        _backupFilePath = Path.Combine(
            directory ?? string.Empty,
            $"{originalFileName}{extension}.reds");
    }

    /// <summary>
    /// 在指定内存地址替换单个字节
    /// </summary>
    /// <param name="hexAddress">十六进制内存地址（支持0x前缀）</param>
    /// <param name="expectedValue">预期的原字节值（十六进制）</param>
    /// <param name="newValue">新的字节值（十六进制）</param>
    /// <returns>是否成功执行替换</returns>
    public bool ReplaceByte(string hexAddress, string expectedValue, string newValue)
    {
        CreateBackupOnce();

        var parsedAddress = TryParseHexWithPrefix(hexAddress, out long address);
        var parsedExpected = TryParseHexByteWithPrefix(expectedValue, out byte expectedByte);
        var parsedNew = TryParseHexByteWithPrefix(newValue, out byte newByte);

        if (!parsedAddress)
        {
            // Log.Error("无效的内存地址格式：{HexAddress}（示例：0x123ABC 或 123ABC）", hexAddress);
            return false;
        }

        if (!parsedExpected)
        {
            // Log.Error("无效的原字节格式：{ExpectedValue}（示例：0x85 或 85）", expectedValue);
            return false;
        }

        if (!parsedNew)
        {
            // Log.Error("无效的新字节格式：{NewValue}（示例：0x31 或 31）", newValue);
            return false;
        }

        try
        {
            using var fileStream = new FileStream(
                _targetFilePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.Read);

            ValidateAddress(fileStream.Length, address);
            fileStream.Position = address;

            var currentByte = (byte)fileStream.ReadByte();

            if (currentByte == newByte)
            {
                // Log.Information("目标地址 {Address:X} 已包含预期值，无需修改", address);
                return true;
            }

            if (currentByte != expectedByte)
            {
                // Log.Error("地址 {Address:X} 的当前值 {Current:X2} 与预期值 {Expected:X2} 不匹配", address, currentByte, expectedByte);
                return false;
            }

            fileStream.Position = address;
            fileStream.WriteByte(newByte);

            // Log.Information($"成功在地址 {address:X} 将 {expectedByte:X2} 修改为 {newByte:X2}");

            return true;
        }
        catch (Exception ex)
        {
            HandleError($"字节替换失败：{ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 替换文件中的特征字节模式
    /// </summary>
    /// <param name="searchPattern">要查找的字节模式（支持0xFF通配符）</param>
    /// <param name="replacementPattern">替换用的字节模式</param>
    /// <param name="allowWildcard">是否启用通配符匹配</param>
    /// <param name="maxReplaceCount">最大替换次数（默认1次）</param>
    /// <returns>是否执行了至少一次替换</returns>
    public bool ReplacePattern(
        byte[] searchPattern,
        byte[] replacementPattern,
        bool allowWildcard = false,
        int maxReplaceCount = 1)
    {
        ValidatePatterns(searchPattern, replacementPattern);

        try
        {
            var modified = false;
            using var fileStream = new FileStream(
                _targetFilePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.Read);

            using var mmf = MemoryMappedFile.CreateFromFile(
                fileStream,
                null,
                fileStream.Length,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false);

            using var accessor = mmf.CreateViewAccessor();
            var fileData = new byte[accessor.Capacity];
            accessor.ReadArray(0, fileData, 0, fileData.Length);

            var matches = FindPatternMatches(fileData, searchPattern, allowWildcard);
            if (matches.Count == 0)
            {
                // Log.Information("未找到匹配的字节模式");
                return false;
            }

            CreateBackupOnce();
            foreach (var index in matches.Take(maxReplaceCount))
            {
                accessor.WriteArray(index, replacementPattern, 0, replacementPattern.Length);
                Log.Debug("在偏移量 {Offset:X} 处完成模式替换", index);
                modified = true;
            }
            return modified;
        }
        catch (Exception ex)
        {
            HandleError($"特征模式替换失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 使用优化的Boyer-Moore算法查找所有匹配位置
    /// </summary>
    private static List<int> FindPatternMatches(
        byte[] source,
        byte[] pattern,
        bool allowWildcard)
    {
        var matches = new List<int>();
        var badCharShift = BuildBadCharTable(pattern, allowWildcard);
        int sourceIndex = 0;

        while (sourceIndex <= source.Length - pattern.Length)
        {
            int patternIndex;
            for (patternIndex = pattern.Length - 1; patternIndex >= 0; patternIndex--)
            {
                if (!IsByteMatch(source[sourceIndex + patternIndex],
                    pattern[patternIndex],
                    allowWildcard))
                {
                    break;
                }
            }

            if (patternIndex < 0)
            {
                matches.Add(sourceIndex);
                sourceIndex += pattern.Length;
            }
            else
            {
                var shift = badCharShift[source[sourceIndex + patternIndex]];
                sourceIndex += Math.Max(1, patternIndex - shift);
            }
        }
        return matches;
    }

    /// <summary>
    /// 构建坏字符跳转表（支持通配符优化）
    /// </summary>
    private static int[] BuildBadCharTable(byte[] pattern, bool allowWildcard)
    {
        var table = new int[256];
        Array.Fill(table, -1);

        for (int i = 0; i < pattern.Length; i++)
        {
            if (allowWildcard && pattern[i] == WildcardByte) continue;
            table[pattern[i]] = i;
        }
        return table;
    }

    /// <summary>
    /// 字节匹配逻辑（支持通配符）
    /// </summary>
    private static bool IsByteMatch(byte source, byte pattern, bool allowWildcard)
    {
        return allowWildcard && pattern == WildcardByte || source == pattern;
    }

    /// <summary>
    /// 创建唯一备份文件（线程安全）
    /// </summary>
    private void CreateBackupOnce()
    {
        if (_backupCreated || File.Exists(_backupFilePath)) return;

        lock (_backupLock) // 使用私有锁对象
        {
            if (_backupCreated) return;

            File.Copy(_targetFilePath, _backupFilePath);
            _backupCreated = true;
            Log.Information($"创建备份文件：{_backupFilePath}");
        }
    }

    #region Validation Methods
    private static void ValidateAddress(long fileLength, long address)
    {
        if (address < 0 || address >= fileLength)
            throw new ArgumentOutOfRangeException(
                nameof(address),
                $"地址 {address:X} 超出文件范围 (0-{fileLength - 1:X})");
    }

    private static void ValidatePatterns(byte[] search, byte[] replace)
    {
        if (search == null || search.Length == 0)
            throw new ArgumentException("搜索模式不能为空", nameof(search));

        if (replace == null || replace.Length == 0)
            throw new ArgumentException("替换模式不能为空", nameof(replace));

        if (search.Length != replace.Length)
            throw new ArgumentException(
                $"模式长度不匹配（搜索：{search.Length}，替换：{replace.Length}）");
    }
    #endregion

    #region Parsing Utilities
    /// <summary>
    /// 解析带0x前缀的十六进制地址
    /// </summary>
    public static bool TryParseHexWithPrefix(string input, out long result)
    {
        var cleanHex = input?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?? false
            ? input[2..]
            : input;

        return long.TryParse(cleanHex,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out result);
    }

    /// <summary>
    /// 解析带0x前缀的十六进制字节
    /// </summary>
    public static bool TryParseHexByteWithPrefix(string input, out byte result)
    {
        var cleanHex = input?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?? false
            ? input[2..]
            : input;

        return byte.TryParse(cleanHex,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out result);
    }
    #endregion

    #region Error Handling
    private void HandleError(string message, Exception ex)
    {
        switch (ex)
        {
            case ArgumentOutOfRangeException:
                Log.Error("地址越界：{Message}", ex.Message);
                break;
            case FileNotFoundException:
                Log.Error("目标文件不存在，请检查路径：{Path}", _targetFilePath);
                break;
            case UnauthorizedAccessException:
                Log.Warning("文件访问权限不足，请使用管理员权限运行");
                break;
            case IOException ioEx when IsSharingViolation(ioEx):
                Log.Error("文件被其他进程占用，请关闭相关程序");
                break;
            default:
                Log.Error("{Message}\n{StackTrace}", message, ex.StackTrace);
                break;
        }
    }

    private static bool IsSharingViolation(IOException ex)
    {
        var errorCode = Marshal.GetHRForException(ex) & 0xFFFF;
        return errorCode == 32; // ERROR_SHARING_VIOLATION
    }
    #endregion
}