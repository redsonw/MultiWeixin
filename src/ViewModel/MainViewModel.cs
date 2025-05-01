using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiWeixin.Assist;
using MultiWeixin.Models;
using Serilog;
using Serilog.Events;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MultiWeixin.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly object _collectionLock = new();
        private readonly object _patchDetailsLock = new();

        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntrys = [];

        [ObservableProperty]
        private string _weChatVersion = string.Empty;

        [ObservableProperty]
        private string _weixinVersion = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _items = [];

        [ObservableProperty]
        private string _weChatWinPath = string.Empty;

        [ObservableProperty]
        private string _weixinWinPath = string.Empty;

        [ObservableProperty]
        private string _libPath = string.Empty;

        public WeChatInfoMonitor WeChatInfoMonitor { get; set; } = new();

        private readonly WeChatConfig? _weChatConfig;

        private Options _weChatOptions = new();

        private readonly JsonParser _jsonParser;

        [ObservableProperty]
        private PatchDetails? _patchDetails;

        [ObservableProperty]
        private bool _weChatEnabled;

        [ObservableProperty]
        private bool _weixinEnabled;

        public MainViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(LogEntrys, _collectionLock);

            _jsonParser = new("https://www.Youdomain.com/WeChat/PatchSignature.json");
            try
            {
                _weChatConfig = WeChatInfoMonitor.GetWechatInfo();

                var weChatVersion = new VersionCodec(uint.TryParse(_weChatConfig?.WeChatVersion, out uint svalue) ? svalue : 0, VersionEncodingRule.ScaleBased);
                var weixinVersion = new VersionCodec(uint.TryParse(_weChatConfig?.WeixinVersion, out uint wvalue) ? wvalue : 0, VersionEncodingRule.OffsetBased);

                _weChatVersion = weChatVersion.ToString();
                _weixinVersion = weixinVersion.ToString();
            }
            catch (Exception ex)
            {
                LogError($"此计算机未安装任何版本的微信，错误信息：{ex.Message}");
            }

            Task.Run(InitializeAsync).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    LogError($"初始化异常: {t.Exception}");
            });
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                LogError($"初始化失败: {ex.Message}");
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                // 先执行异步操作，获取结果
                var patchDetails = await _jsonParser.ParseAsync();

                // 在同步块中处理结果
                lock (_patchDetailsLock)
                {
                    PatchDetails = patchDetails;
                }

                if (PatchDetails == null || PatchDetails.WeChat.Version.Count == 0)
                {
                    LogError("获取特征码信息失败，请联系作者反馈。");
                    return;
                }

                var versionProcessors = new List<(string Version, string InstallPath, string VersionType, string RelativePath, string LogPrefix, Action<bool> SetEnabled, Action<string> winDllPath)>
                {
                    (
                        Version: WeChatVersion,
                        InstallPath: _weChatConfig?.WeChatInstallPath ?? string.Empty,
                        VersionType: "v3.9.x 系列",
                        RelativePath: $"[{WeChatVersion}]\\WeChatWin.dll",
                        LogPrefix: "微信",
                        SetEnabled: enabled => WeChatEnabled = enabled,
                        winDllPath: lib => WeChatWinPath = lib
                    ),
                    (
                        Version: WeixinVersion,
                        InstallPath: _weChatConfig?.WeixinInstallPath ?? string.Empty,
                        VersionType: "v4.0.3.x 系列",
                        RelativePath: $"{WeixinVersion}\\Weixin.dll",
                        LogPrefix: "微信",
                        SetEnabled: enabled => WeixinEnabled = enabled,
                        winDllPath:lib => WeixinWinPath = lib
                    )
                };

                var tasks = versionProcessors.Select(vp => Task.Run(() => ProcessVersion(vp.Version, vp.InstallPath, vp.VersionType, vp.RelativePath, vp.LogPrefix, vp.SetEnabled, vp.winDllPath)));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                LogError($"加载补丁信息失败: {ex.Message}");
            }
        }

        private void ProcessVersion(string version, string installPath, string versionType, string relativePath, string logPrefix, Action<bool> setEnabled, Action<string> winDllPath)
        {

            if (!CheckVersionSupport(version, logPrefix, versionType, setEnabled))
                return;

            if (!ValidateInstallPath(installPath))
                return;

            SetVersionStatus(version, installPath, relativePath, logPrefix, setEnabled, winDllPath);
        }

        private static bool CheckVersionSupport(string version, string logPrefix, string versionType, Action<bool> setEnabled)
        {
            if (string.IsNullOrEmpty(version) || version == "0.0.0.0")
            {
                LogWarning($"{logPrefix} {versionType} 未安装 ...");
                setEnabled(false);
                return false;
            }
            return true;
        }

        private static bool ValidateInstallPath(string installPath)
        {
            if (string.IsNullOrEmpty(installPath))
            {
                LogWarning("微信安装路径未配置，无法验证文件");
                return false;
            }

            return true;
        }

        private void SetVersionStatus(string version, string installPath, string relativePath, string logPrefix, Action<bool> setEnabled, Action<string> winDllPath)
        {
            var fullPath = Path.Combine(installPath, relativePath);
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                LogWarning($"无效的 {logPrefix} 安装路径，如使用的是绿色版微信请安装官方版本。");
                setEnabled(false);
                return;
            }

            lock (_patchDetailsLock)
            {
                if (PatchDetails?.WeChat.Version.ContainsKey(version) != true)
                {
                    LogError($"{logPrefix} {version} 还未支持，请联系作者反馈。");
                    setEnabled(false);
                    return;
                }
            }

            var verInfo = PatchDetails?.WeChat.Version[version];

            BinaryFilePatcher.TryParseHexWithPrefix(verInfo.Offset, out long offset);
            var newByte = ReadByteFromFile(fullPath, offset);
            BinaryFilePatcher.TryParseHexByteWithPrefix(verInfo.NewValue, out byte fByte);

            LogDebug($"在 {offset:X} 处找到了 {newByte:X2}，要更新的值为：{verInfo.NewValue:X2}");



            if (newByte == fByte)
            {
                LogWarning($"{logPrefix} {version} 已经解除多过开限制，无需重复操作。");
                setEnabled(false);
                return;
            }

            winDllPath(fullPath);
            setEnabled(true);
            LogInfo($"{logPrefix} {version} 已安装并支持。");
        }

        private static byte ReadByteFromFile(string path, long offset)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(offset, SeekOrigin.Begin);
            return (byte)fs.ReadByte();
        }

        [RelayCommand]
        public void Patcher()
        {
            if (WeixinEnabled)
            {
                PatchClient(WeixinWinPath, WeixinVersion);
            }

            if (WeChatEnabled)
            {
                PatchClient(WeChatWinPath, WeChatVersion);
            }
        }

        private void PatchClient(string targetPath, string version)
        {
            try
            {
                PatchDetails? patchDetails;
                lock (_patchDetailsLock)
                {
                    patchDetails = PatchDetails;
                }

                var versionDetails = patchDetails?.WeChat?.Version[version];
                if (versionDetails == null)
                {
                    LogWarning("暂不支持此版本，请联系作者催更：https://www.redsonw.com/MultiWeixin.html");
                    return;
                }

                var options = new Options
                {
                    FilePath = targetPath,
                    OffsetByte = versionDetails.Offset,
                    OldValue = versionDetails.OldValue,
                    NewValue = versionDetails.NewValue
                };

                if (string.IsNullOrEmpty(options.FilePath))
                {
                    LogWarning($"关键文件路径为空，无法继续执行操作，请查检路径是否正确：{options.FilePath}");
                    return;
                }

                if (string.IsNullOrEmpty(options.OffsetByte) || string.IsNullOrEmpty(options.OldValue) || string.IsNullOrEmpty(options.NewValue))
                {
                    LogWarning("补丁信息获取失败，请关闭后重试。");
                    return;
                }

                var patcher = new BinaryFilePatcher(options.FilePath);
                if (patcher.ReplaceByte(options.OffsetByte, options.OldValue, options.NewValue))
                {
                    LogInfo($"微信 v{version} 解除双开限制成功...");
                }
            }
            catch (Exception ex)
            {
                LogError($"修补客户端失败: {ex.Message}");
            }
        }



        partial void OnWeixinVersionChanged(string value) => WeixinEnabled = value != "未安装" && value != "0.0.0.0";

        partial void OnWeChatVersionChanged(string value) => WeChatEnabled = value != "未安装" && value != "0.0.0.0";

        partial void OnWeixinEnabledChanged(bool value) => HandleEnabledChanged(value, WeixinVersion, "微信");

        partial void OnWeChatEnabledChanged(bool value) => HandleEnabledChanged(value, WeChatVersion, "微信");

        private static void HandleEnabledChanged(bool enabled, string version, string appName)
        {
            if (enabled)
            {
                if (version == "未安装" || version == "0.0.0.0")
                {
                    Log.Warning($"{appName} {version} 不可用。");
                }
                else
                {
                    Log.Information($"{appName} {version} 可用。");
                }
            }
            else
            {
                Log.Warning($"{appName} {version} 不可用。");
            }
        }

        public void AddLog(string message, LogEventLevel level, Brush logForeground, Brush logBackground)
        {
            Brush safeForeground = new SolidColorBrush(((SolidColorBrush)logForeground).Color);
            Brush safeBackground = new SolidColorBrush(((SolidColorBrush)logBackground).Color);

            safeForeground.Freeze();
            safeBackground.Freeze();

            var log = new LogEntry
            {
                LogMessage = message,
                Level = level,
                LogForeground = safeForeground,
                LogBackground = safeBackground
            };


            lock (_collectionLock)
            {
                LogEntrys.Insert(0, log);
            }

        }

        [RelayCommand]
        private static void CloseApp() => Application.Current.Shutdown();

        public static void LogDebug(string message) => Log.Debug(message);

        public static void LogInfo(string message) => Log.Information(message);

        public static void LogVerbose(string message) => Log.Verbose(message);

        public static void LogError(string message) => Log.Error(message);

        public static void LogWarning(string message) => Log.Warning(message);

        public static void LogFatal(string message) => Log.Fatal(message);
    }
}