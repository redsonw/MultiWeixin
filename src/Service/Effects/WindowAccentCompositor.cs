﻿using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MultiWeixin.Service.Effects
{
    /// <summary>
    /// 为窗口提供模糊特效。
    /// </summary>
    /// <remarks>
    /// 创建 <see cref="WindowAccentCompositor"/> 的一个新实例。
    /// </remarks>
    /// <param name="window">要创建模糊特效的窗口实例。</param>
    public class WindowAccentCompositor(Window window)
    {
        private readonly Window _window = window ?? throw new ArgumentNullException(nameof(window));

        /// <summary>
        /// 窗口颜色增强。
        /// </summary>
        /// <param name="color"></param>
        public void Composite(Color color)
        {
            Window window = _window;
            var handle = new WindowInteropHelper(window).EnsureHandle();

            var gradientColor =
                // 组装红色分量。
                color.R << 0 |
                // 组装绿色分量。
                color.G << 8 |
                // 组装蓝色分量。
                color.B << 16 |
                // 组装透明分量。
                color.A << 24;

            Composite(handle, gradientColor);
        }

        private static void Composite(nint handle, int color)
        {
            // 创建 AccentPolicy 对象。
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = 0,
            };

            // 将托管结构转换为非托管对象。
            var accentPolicySize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentPolicySize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            // 设置窗口组合特性。
            try
            {
                // 设置模糊特效。
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentPolicySize,
                    Data = accentPtr,
                };
                SetWindowCompositionAttribute(handle, ref data);
            }
            finally
            {
                // 释放非托管对象。
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(nint hwnd, ref WindowCompositionAttributeData data);

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public nint Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            // 省略其他未使用的字段
            WCA_ACCENT_POLICY = 19,
            // 省略其他未使用的字段
        }
    }
}