using System;
using System.Runtime.InteropServices;

namespace TouchCloudPad
{
    /// <summary>
    /// Win32 interop used by the overlay window: window styles, glass/acrylic
    /// composition, always-on-top and click-through behaviour.
    /// </summary>
    internal static class Native
    {
        // ---- Window styles (extended) ----
        public const int GWL_EXSTYLE = -20;
        public const uint WS_EX_TRANSPARENT = 0x00000020; // pass-through mouse
        public const uint WS_EX_NOACTIVATE  = 0x08000000; // never steal keyboard focus
        public const uint WS_EX_LAYERED     = 0x00080000;
        public const uint WS_EX_TOOLWINDOW  = 0x00000080; // hide from Alt-Tab / taskbar
        public const int SWP_NOSIZE    = 0x0001;
        public const int SWP_NOMOVE    = 0x0002;
        public const int SWP_NOZORDER  = 0x0004;
        public const int SWP_NOACTIVATE= 0x0010;
        public const int SWP_SHOWWINDOW= 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        // ---- Acrylic / blur backdrop (Windows 10) ----
        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor; // ABGR (for acrylic tint)
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        // Accent states
        public const int ACCENT_DISABLED                   = 0;
        public const int ACCENT_ENABLE_BLURBEHIND          = 3;
        public const int ACCENT_ENABLE_ACRYLICBLURBEHIND   = 4;

        public const int WCA_ACCENT_POLICY = 19;

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd,
            ref WindowCompositionAttributeData data);

        /// <summary>
        /// Applies the Win10 acrylic-blur glass backdrop to a window handle.
        /// On Windows 10 1803+ this gives a frosted-glass effect; older builds
        /// silently fall back to a plain blur. No-op if the API is unavailable.
        /// </summary>
        public static void ApplyAcrylic(IntPtr hwnd, bool enabled,
            uint tintColor /* ABGR, 0 = default */, bool addBorder)
        {
            try
            {
                var accent = new AccentPolicy();
                if (enabled)
                {
                    if (Environment.OSVersion.Version >= new Version(10, 0, 17134))
                        accent.AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND;
                    else
                        accent.AccentState = ACCENT_ENABLE_BLURBEHIND;
                    accent.GradientColor = (int)tintColor;
                    if (addBorder) accent.AccentFlags = 2; // draws a 1px border
                }
                else
                {
                    accent.AccentState = ACCENT_DISABLED;
                }

                int size = Marshal.SizeOf(accent);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(accent, ptr, false);
                    var data = new WindowCompositionAttributeData
                    {
                        Attribute = WCA_ACCENT_POLICY,
                        Data = ptr,
                        SizeOfData = size
                    };
                    SetWindowCompositionAttribute(hwnd, ref data);
                }
                finally { Marshal.FreeHGlobal(ptr); }
            }
            catch { /* older Windows: ignore */ }
        }

        // ---- Misc ----
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
    }
}
