using System.Runtime.InteropServices;
using System.Text;

namespace MultiWeixin.Service
{
    /// <summary>
    /// Windows 窗口服务
    /// </summary>
    public class WindowService
    {
        /// <summary>
        /// Import the necessary Windows API functions
        /// </summary>
        /// <param name="lpClassName"></param>
        /// <param name="lpWindowName"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern nint FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern nint FindWindowEx(nint hwndParent, nint hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumChildWindows(nint hWndParent, EnumChildProc lpEnumFunc, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern nint GetFocus();

        /// <summary>
        /// Define the RECT structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            /// <summary>
            /// 左边
            /// </summary>
            public int Left;
            /// <summary>
            /// 顶部
            /// </summary>
            public int Top;
            /// <summary>
            /// 右边
            /// </summary>
            public int Right;
            /// <summary>
            /// 底部
            /// </summary>
            public int Bottom;
        }

        /// <summary>
        /// Define the delegate for the EnumChildWindows callback function
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public delegate bool EnumChildProc(nint hWnd, nint lParam);
    }
}
