using Serilog.Core;
using Serilog.Events;
using MultiWeixin.ViewModel;
using System.Windows.Media;

namespace MultiWeixin.Assist;

public class LogSink(MainViewModel mainViewModel) : ILogEventSink
{
    // private readonly MainViewModel _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

    // Emit 方法用于接收日志事件
    public void Emit(LogEvent logEvent)
    {
        // 提取日志等级和信息
        var level = logEvent.Level;
        var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ {GetLocalizedLevel(level)} ] {logEvent.RenderMessage()}";
        (var logForeground, var logBackground) = GetColors(level);

        // 更新 MainViewModel 中的日志
        mainViewModel.AddLog(message, level, logForeground, logBackground);
    }

    public static (Brush fg, Brush bg) GetColors(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => ((Brush fg, Brush bg))(new SolidColorBrush(Colors.DarkGray), new SolidColorBrush(Colors.Transparent)),
            LogEventLevel.Debug => ((Brush fg, Brush bg))(new SolidColorBrush(Colors.Blue), new SolidColorBrush(Colors.Transparent)),
            LogEventLevel.Information => ((Brush fg, Brush bg))(new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.Transparent)),
            LogEventLevel.Warning => ((Brush fg, Brush bg))(new SolidColorBrush(Color.FromArgb(0xFF,0xFF,0xA6,0x0F)), new SolidColorBrush(Colors.Transparent)),
            LogEventLevel.Error => ((Brush fg, Brush bg))(new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.Transparent)),
            LogEventLevel.Fatal => ((Brush fg, Brush bg))(new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.Transparent)),
            _ => ((Brush fg, Brush bg))(new SolidColorBrush(Colors.Gray), new SolidColorBrush(Colors.Transparent)),
        };
    }

    public static string GetLocalizedLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "详细",
            LogEventLevel.Debug => "调试",
            LogEventLevel.Information => "信息",
            LogEventLevel.Warning => "警告",
            LogEventLevel.Error => "错误",
            LogEventLevel.Fatal => "致命",
            _ => level.ToString(),
        };
    }
}
