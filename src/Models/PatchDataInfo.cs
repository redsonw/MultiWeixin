namespace MultiWeixin.Models
{
    /// <summary>
    /// 表示微信补丁的详细信息，包含微信程序的具体补丁数据。
    /// </summary>
    public class PatchDetails
    {
        /// <summary>
        /// 获取或设置微信程序的补丁信息。
        /// </summary>
        public WeChat WeChat { get; set; } = new();
    }

    /// <summary>
    /// 表示微信程序的补丁信息，包含不同版本的补丁详情。
    /// </summary>
    public class WeChat
    {
        /// <summary>
        /// 获取或设置一个字典，其中包含版本号和对应的版本补丁详情。
        /// </summary>
        public Dictionary<string, VersionDetail> Version { get; set; } = [];
    }

    /// <summary>
    /// 表示特定版本的补丁详情，包括内存偏移量、旧值和新值。
    /// </summary>
    public class VersionDetail(string offset, string oldValue, string newValue)
    {

        /// <summary>
        /// 获取或设置补丁应用的内存偏移量。
        /// </summary>
        public string Offset { get; set; } = offset;

        /// <summary>
        /// 获取或设置补丁应用前的原始值。
        /// </summary>
        public string OldValue { get; set; } = oldValue;

        /// <summary>
        /// 获取或设置补丁应用后的新值。
        /// </summary>
        public string NewValue { get; set; } = newValue;
    }
}
