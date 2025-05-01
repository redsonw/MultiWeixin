using Serilog.Events;
using System.Windows.Media;

namespace MultiWeixin.Models
{
    public class LogEntry
    {
        public string LogMessage { get; set; } = string.Empty;
        public LogEventLevel Level { get; set; }
        // public DateTime Timestamp { get; set; }
        public Brush LogForeground { get; set; } = Brushes.Black;
        public Brush LogBackground { get; set; } = Brushes.Transparent;
    }
}
