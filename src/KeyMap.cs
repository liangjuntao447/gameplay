using System;
using System.Collections.Generic;

namespace TouchCloudPad
{
    /// <summary>
    /// Maps friendly keyboard-key names (as used by on-screen buttons) to Win32
    /// virtual-key codes. These are distinct from the gamepad pad names, so a
    /// button can be bound to EITHER a pad (gamepad) or a key (keyboard, e.g. Esc).
    /// </summary>
    internal static class KeyMap
    {
        private static readonly Dictionary<string, ushort> M =
            new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);

        static KeyMap()
        {
            M["Esc"] = 0x1B; M["Escape"] = 0x1B;
            M["Space"] = 0x20;
            M["Enter"] = 0x0D; M["Return"] = 0x0D;
            M["Tab"] = 0x09;
            M["Up"] = 0x26; M["Down"] = 0x28; M["Left"] = 0x25; M["Right"] = 0x27;
            for (int i = 0; i <= 9; i++) M[i.ToString()] = (ushort)(0x30 + i);
            for (int i = 1; i <= 12; i++) M["F" + i] = (ushort)(0x70 + i - 1);
        }

        public static int TryGet(string name)
        {
            if (name == null) return -1;
            ushort v;
            if (M.TryGetValue(name.Trim(), out v)) return v;
            return -1;
        }

        public static bool Known(string name) { return TryGet(name) >= 0; }

        /// <summary>Common assignable keyboard keys (shown in the settings UI).</summary>
        public static readonly string[] Names =
        {
            "Esc", "Space", "Enter", "Tab",
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
            "Up", "Down", "Left", "Right"
        };
    }
}
