using MultiWeixin.Service.Effects;
using MultiWeixin.ViewModel;
using Serilog;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;

namespace MultiWeixin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; }
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;

            DataContext = this.ViewModel;

            var compositor = new WindowAccentCompositor(this);
            var solidColorBrush = (SolidColorBrush)FindResource("BackgroundBrush");
            var background = solidColorBrush.Color;

            compositor.Composite(background);

            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = default,
                GlassFrameThickness = new Thickness(0, 1, 0, 0),
                ResizeBorderThickness = ResizeMode == ResizeMode.NoResize ? default : new Thickness(4),
                // UseAeroCaptionButtons = true,
                NonClientFrameEdges = NonClientFrameEdges.Left | NonClientFrameEdges.Right | NonClientFrameEdges.Bottom
            });

        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 检查窗口是否为最大化状态，若是，则先恢复成正常状态
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }

                // 允许拖动窗口
                this.DragMove();
            }
            catch
            {
                // 避免异常干扰程序
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("准备就绪...");
            // ViewModel.LoadAsync();
        }

        private void ListBoxItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                // 获取 AlternationIndex（可选，用于动态延迟）
                int index = (int)item.GetValue(ItemsControl.AlternationIndexProperty);
                TimeSpan delay = TimeSpan.FromMilliseconds(index * 50); // 每项延迟 50ms

                // 创建 Storyboard
                Storyboard storyboard = new Storyboard();

                // Opacity 动画
                DoubleAnimation opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                    BeginTime = delay
                };
                Storyboard.SetTarget(opacityAnimation, item);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

                // 位移动画
                DoubleAnimation translateAnimation = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                    BeginTime = delay
                };
                translateAnimation.EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 };
                Storyboard.SetTarget(translateAnimation, item);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("RenderTransform.X"));

                // 添加动画到 Storyboard
                storyboard.Children.Add(opacityAnimation);
                storyboard.Children.Add(translateAnimation);

                // 启动动画
                storyboard.Begin();
            }
        }
    }
}