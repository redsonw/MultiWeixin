namespace MultiWeixin.Models
{
    public class WeChatConfig
    {
        public string? WeChatVersion { get; set; }
        public string WeChatInstallPath { get; set; } = string.Empty;
        public string WeChatFileSavePath { get; set; } = string.Empty;
        public string? WeixinVersion { get; set; }
        public string WeixinInstallPath { get; set; } = string.Empty;
        public string WeixinFileSavePath { get; set; } = string.Empty;
    }
}
