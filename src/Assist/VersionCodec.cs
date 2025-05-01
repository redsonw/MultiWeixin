using Serilog;

namespace MultiWeixin.Assist;

/// <summary>
/// 版本编码规则枚举：
/// OffsetBased - 微信版本号（如 4.0.2.26）的偏移量编码规则
/// ScaleBased - 另一种版本（如 3.9.12.45）的比例缩放编码规则
/// </summary>
public enum VersionEncodingRule
{
    OffsetBased,
    ScaleBased
}

/// <summary>
/// 微信版本号编解码器，支持偏移量和比例缩放两种编码规则
/// </summary>
public class VersionCodec
{
    // 版本号组成部分
    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Build { get; private set; }
    public int Revision { get; private set; }

    private readonly VersionEncodingRule _encodingRule;

    #region ScaleBased 规则常量（比例缩放）
    private const int MajorScale = 33;
    private const int MinorScale = 1;
    private const int BuildScale = 1;
    private const int RevisionScale = 1;
    #endregion

    #region OffsetBased 规则常量（偏移量）
    private const int MajorOffset = 238;
    private const int MinorOffset = 84;
    #endregion

    /// <summary>
    /// 通过版本字符串初始化编解码器
    /// </summary>
    /// <param name="version">格式: major.minor.build.revision</param>
    /// <param name="rule">版本编码规则</param>
    public VersionCodec(string version, VersionEncodingRule rule)
    {
        _encodingRule = rule;
        ParseVersionString(version);
    }

    /// <summary>
    /// 通过编码后的整型版本号初始化编解码器（支持大数值）
    /// </summary>
    public VersionCodec(uint encodedVersion, VersionEncodingRule rule)
    {
        _encodingRule = rule;
        DecodeFromInteger(encodedVersion);
    }

    /// <summary>
    /// 将编码后的整型版本号解码为语义化版本
    /// </summary>
    private void DecodeFromInteger(uint? encodedVersion)
    {
        if (encodedVersion == null)
        {
            throw new ArgumentNullException(nameof(encodedVersion), "编码版本号不能为空");
        }

        uint ev = encodedVersion.Value;
        int encodedMajor = (int)((ev >> 24) & 0xFF);
        int encodedMinor = (int)((ev >> 16) & 0xFF);
        int encodedBuild = (int)((ev >> 8) & 0xFF);
        int encodedRevision = (int)(ev & 0xFF);

        if (_encodingRule == VersionEncodingRule.OffsetBased)
        {
            Major = encodedMajor - MajorOffset;
            Minor = encodedMinor - MinorOffset;
            Build = encodedBuild;
            Revision = encodedRevision;

            if (Major < 0 || Minor < 0)
            {
                throw new ArgumentException("解码后的 Major 或 Minor 为负数，无效的编码版本号");
            }
        }
        else
        {
            if (encodedMajor % MajorScale != 0 || encodedMinor % MinorScale != 0 ||
                encodedBuild % BuildScale != 0 || encodedRevision % RevisionScale != 0)
            {
                throw new ArgumentException("编码版本号不能被缩放因子整除，无效的编码版本号");
            }

            Major = encodedMajor / MajorScale;
            Minor = encodedMinor / MinorScale;
            Build = encodedBuild / BuildScale;
            Revision = encodedRevision / RevisionScale;
        }
    }

    /// <summary>
    /// 将版本字符串解析为语义化版本
    /// </summary>
    private void ParseVersionString(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 4)
        {
            throw new ArgumentException("无效的版本格式，必须为 major.minor.build.revision");
        }

        try
        {
            Major = int.Parse(parts[0]);
            Minor = int.Parse(parts[1]);
            Build = int.Parse(parts[2]);
            Revision = int.Parse(parts[3]);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("非法的版本号格式", ex);
        }
    }

    /// <summary>
    /// 将当前版本编码为无符号整型版本号（支持大数值输出）
    /// </summary>
    public uint EncodeToInteger()
    {
        uint encodedMajor, encodedMinor, encodedBuild, encodedRevision;

        if (_encodingRule == VersionEncodingRule.OffsetBased)
        {
            encodedMajor = (uint)(Major + MajorOffset);
            encodedMinor = (uint)(Minor + MinorOffset);
            encodedBuild = (uint)Build;
            encodedRevision = (uint)Revision;
        }
        else
        {
            encodedMajor = (uint)(Major * MajorScale);
            encodedMinor = (uint)(Minor * MinorScale);
            encodedBuild = (uint)(Build * BuildScale);
            encodedRevision = (uint)(Revision * RevisionScale);
        }

        // 确保每个部分的值都在0-255范围内
        if (encodedMajor > 255 || encodedMinor > 255 || encodedBuild > 255 || encodedRevision > 255)
        {
            throw new OverflowException("版本号超出可编码范围");
        }

        // 使用uint避免符号扩展问题
        return (encodedMajor << 24)
            | (encodedMinor << 16)
            | (encodedBuild << 8)
            | encodedRevision;
    }

    /// <summary>
    /// 获取语义化版本字符串表示
    /// </summary>
    public override string ToString()
    {
        return $"{Major}.{Minor}.{Build}.{Revision}";
    }
}