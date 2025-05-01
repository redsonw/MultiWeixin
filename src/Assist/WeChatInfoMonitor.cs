using Microsoft.Win32;
using MultiWeixin.Models;
using Serilog;
using System.Windows;

namespace MultiWeixin.Assist
{
    public class WeChatInfoMonitor
    {
        private const string RegistRoot = @"HKEY_CURRENT_USER\Software\Tencent";
        private const string WeChatSubKey = "WeChat";
        private const string WeixinSubKey = "Weixin";

        private class ValueName
        {
            public const string Version = "Version";
            public const string InstallPath = "InstallPath";
            public const string FileSavePath = "FileSavePath";
        }

        private static string GetValue(string subKey, string valueName)
        {
            var fullName = $"{RegistRoot}\\{subKey}";
            object value = Registry.GetValue(fullName, valueName, null)!;

            string result = value switch
            {
                // 关键修复：将DWORD转为无符号再转字符串
                int dword => unchecked((uint)dword).ToString(),
                string str => str,
                byte[] bytes when bytes.Length == 4 =>
                    BitConverter.ToUInt32(bytes, 0).ToString(), // 处理二进制格式DWORD
                _ => string.Empty
            };

            Log.Verbose($"{result}");
            return result;
        }

        public static WeChatConfig GetWechatInfo()
        {
            return new WeChatConfig()
            {
                WeChatVersion = GetValue(WeChatSubKey, ValueName.Version),
                WeChatInstallPath = GetValue(WeChatSubKey, ValueName.InstallPath),
                WeChatFileSavePath = GetValue(WeChatSubKey, ValueName.FileSavePath),
                WeixinVersion = GetValue(WeixinSubKey, ValueName.Version),
                WeixinInstallPath = GetValue(WeixinSubKey, ValueName.InstallPath)
            };
        }
    }
}
