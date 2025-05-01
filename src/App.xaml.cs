using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiWeixin.Assist;
using MultiWeixin.ViewModel;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Windows;

namespace MultiWeixin
{
    public partial class App : Application
    {
        private static readonly IHost _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
            }).Build();

        public static MainViewModel MainViewModel => _host.Services.GetRequiredService<MainViewModel>();

        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(new ChineseLogFormatter(), $"logs/{DateTime.Now:yyyy-MM-dd}.log",
                        fileSizeLimitBytes: 10 * 1024 * 1024,  // 限制每个日志文件最大10MB
                        rollOnFileSizeLimit: true,  // 超过大小自动创建新文件
                        retainedFileCountLimit: 7,
                        shared: true)  // 只保留最近7个日志文件
                .WriteTo.Sink(new LogSink(MainViewModel)) // 将 MainViewModel 传递给 LogSink
                .MinimumLevel.Information()
                // .Enrich.With(new SensitiveDataEnricher())
                .CreateLogger();
        }



        // 应用程序关闭时关闭日志
        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                _host.Start();
                var app = new App();
                app.InitializeComponent();

                app.MainWindow = _host.Services.GetRequiredService<MainWindow>();
                app.MainWindow.Visibility = Visibility.Visible;
                app.Run();
            }
            finally
            {
                Log.CloseAndFlush();
                _host.Dispose();
            }
        }
    }
}
