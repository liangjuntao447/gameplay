using System;
using System.Runtime.InteropServices;

namespace TouchCloudPad
{
    /// <summary>
    /// System-wide keyboard emulation via SendInput. Used for buttons that send
    /// keyboard keys (e.g. ESC), which work regardless of the gamepad output.
    /// Injected keys go to the focused window (the cloud game), which stays
    /// focused because the overlay window is WS_EX_NOACTIVATE.
    /// </summary>
    internal static class Input
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MAPVK_VK_TO_VSC = 0;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private static void SendVk(ushort vk, uint flags)
        {
            ushort scan = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);
            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = scan, dwFlags = flags } }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void KeyDown(ushort vk) { SendVk(vk, 0); }
        public static void KeyUp(ushort vk) { SendVk(vk, KEYEVENTF_KEYUP); }
        public static void TapKey(ushort vk) { KeyDown(vk); KeyUp(vk); }
    }
}
