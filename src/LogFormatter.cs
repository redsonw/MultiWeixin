using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace MultiWeixin
{
    public class ChineseLogFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var levelInChinese = logEvent.Level switch
            {
                LogEventLevel.Verbose => "详细",
                LogEventLevel.Debug => "调试",
                LogEventLevel.Information => "信息",
                LogEventLevel.Warning => "警告",
                LogEventLevel.Error => "错误",
                LogEventLevel.Fatal => "严重",
                _ => logEvent.Level.ToString()
            };

            output.Write($"{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss} [{levelInChinese}] {logEvent.RenderMessage()}\n");

            if (logEvent.Exception != null)
            {
                output.Write(logEvent.Exception);
            }
        }
    }
}
