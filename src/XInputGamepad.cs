using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TouchCloudPad
{
    /// <summary>
    /// Outputs a virtual Xbox 360 controller (XInput) through the ViGEmBus
    /// kernel driver. This is the standard, broadly supported way to appear as
    /// a real gamepad to PC apps and cloud-game clients.
    ///
    /// Two pieces are needed:
    ///   1. The ViGEmBus DRIVER (install it once; the app checks the service).
    ///   2. The official ViGEmClient.dll, built from source (the GitHub repo is
    ///      source-only; you compile it with CMake/vcpkg, then put the x64
    ///      ViGEmClient.dll next to TouchCloudPad.exe — the app finds it here
    ///      or in System32).
    /// The driver install does NOT ship ViGEmClient.dll, so this app never
    /// downloads it: it just loads the compiled DLL the user provides.
    /// </summary>
    public class XInputGamepad : IDisposable
    {
        private const string DLL = "ViGEmClient.dll";
        private IntPtr _client = IntPtr.Zero;
        private IntPtr _target = IntPtr.Zero;
        private bool _ok;
        private XUSB_REPORT _rep;

        // ---- XUSB (XInput) report, exactly 12 bytes ----
        [StructLayout(LayoutKind.Sequential)]
        private struct XUSB_REPORT
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        // ---- XInput button bitmask ----
        public const ushort BTN_DPAD_UP = 0x0001;
        public const ushort BTN_DPAD_DOWN = 0x0002;
        public const ushort BTN_DPAD_LEFT = 0x0004;
        public const ushort BTN_DPAD_RIGHT = 0x0008;
        public const ushort BTN_START = 0x0010;
        public const ushort BTN_BACK = 0x0020;
        public const ushort BTN_LTHUMB = 0x0040;
        public const ushort BTN_RTHUMB = 0x0080;
        public const ushort BTN_LSHOULDER = 0x0100;
        public const ushort BTN_RSHOULDER = 0x0200;
        public const ushort BTN_GUIDE = 0x0400;
        public const ushort BTN_A = 0x1000;
        public const ushort BTN_B = 0x2000;
        public const ushort BTN_X = 0x4000;
        public const ushort BTN_Y = 0x8000;

        // ---- ViGEmClient C API (official) ----
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vigem_alloc(out IntPtr c);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vigem_connect(IntPtr c);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void vigem_disconnect(IntPtr c);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void vigem_free(IntPtr c);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vigem_target_x360_alloc(out IntPtr t);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vigem_target_add(IntPtr c, IntPtr t);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void vigem_target_remove(IntPtr c, IntPtr t);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void vigem_target_free(IntPtr t);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vigem_target_x360_update(IntPtr c, IntPtr t, ref XUSB_REPORT r);

        public bool Available { get { return _ok; } }

        /// <summary>Human-readable reason why the gamepad is unavailable, or null if OK.</summary>
        public string Error { get; private set; }

        /// <summary>Where the compiled client library must live. The exe folder is checked first.</summary>
        private static string FindClientDll()
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ViGEmClient.dll"),
                Path.Combine(Environment.SystemDirectory, "ViGEmClient.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "ViGEmClient.dll"),
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return c;
            return null;
        }

        public XInputGamepad()
        {
            Init();
            if (!_ok) Cleanup();
        }

        private bool Init()
        {
            try
            {
                if (FindClientDll() == null)
                {
                    Error = "缺少 ViGEmClient.dll：请把用官方源码(CMake/vcpkg)编译好的 x64 ViGEmClient.dll 放到 TouchCloudPad.exe 旁";
                    return false;
                }
                if (vigem_alloc(out _client) != 0) { Error = "ViGEm 初始化失败（请确认是官方源码编译版 x64）"; return false; }
                if (vigem_connect(_client) != 0) { Error = "ViGEmBus 驱动未安装或未加载"; return false; }
                if (vigem_target_x360_alloc(out _target) != 0) { Error = "创建虚拟手柄失败"; return false; }
                if (vigem_target_add(_client, _target) != 0) { Error = "添加虚拟手柄失败"; return false; }
                _ok = true;
                Error = null;
                return true;
            }
            catch { Error = "加载 ViGEmClient.dll 异常"; return false; }
        }

        private void Cleanup()
        {
            try { if (_target != IntPtr.Zero) { vigem_target_remove(_client, _target); vigem_target_free(_target); _target = IntPtr.Zero; } } catch { }
            try { if (_client != IntPtr.Zero) { vigem_disconnect(_client); vigem_free(_client); _client = IntPtr.Zero; } } catch { }
        }

        public void SetButton(ushort bit, bool on)
        {
            if (!_ok) return;
            if (on) _rep.wButtons |= bit; else _rep.wButtons &= (ushort)~bit;
        }

        /// <summary>Set a shoulder trigger. right=false -> LT, right=true -> RT.</summary>
        public void SetTrigger(bool right, bool on)
        {
            if (!_ok) return;
            byte v = (byte)(on ? 255 : 0);
            if (right) _rep.bRightTrigger = v; else _rep.bLeftTrigger = v;
        }

        /// <summary>Set a thumbstick (right=false -> left stick). Values -32768..32767.</summary>
        public void SetStick(bool right, short x, short y)
        {
            if (!_ok) return;
            if (right) { _rep.sThumbRX = x; _rep.sThumbRY = y; }
            else { _rep.sThumbLX = x; _rep.sThumbLY = y; }
        }

        public void Update()
        {
            if (!_ok) return;
            vigem_target_x360_update(_client, _target, ref _rep);
        }

        public void Reset()
        {
            _rep = new XUSB_REPORT();
            Update();
        }

        public void Dispose()
        {
            if (_ok) { try { vigem_target_x360_update(_client, _target, ref _rep); } catch { } Cleanup(); _ok = false; }
        }
    }

    /// <summary>
    /// Maps friendly pad-button names (as used in skins) to XInput buttons /
    /// triggers.
    /// </summary>
    internal static class Pad
    {
        public static bool IsTrigger(string name)
        {
            return name != null && (name == "LT" || name == "RT");
        }

        public static bool IsRightTrigger(string name)
        {
            return name == "RT";
        }

        /// <summary>Bitmask for a button name; 0 if it is a trigger or unknown.</summary>
        public static ushort Bit(string name)
        {
            if (name == null) return 0;
            switch (name.ToUpperInvariant())
            {
                case "A": return XInputGamepad.BTN_A;
                case "B": return XInputGamepad.BTN_B;
                case "X": return XInputGamepad.BTN_X;
                case "Y": return XInputGamepad.BTN_Y;
                case "LB": return XInputGamepad.BTN_LSHOULDER;
                case "RB": return XInputGamepad.BTN_RSHOULDER;
                case "START": return XInputGamepad.BTN_START;
                case "BACK": return XInputGamepad.BTN_BACK;
                case "GUIDE": return XInputGamepad.BTN_GUIDE;
                case "L3": return XInputGamepad.BTN_LTHUMB;
                case "R3": return XInputGamepad.BTN_RTHUMB;
                case "DPAD_UP": return XInputGamepad.BTN_DPAD_UP;
                case "DPAD_DOWN": return XInputGamepad.BTN_DPAD_DOWN;
                case "DPAD_LEFT": return XInputGamepad.BTN_DPAD_LEFT;
                case "DPAD_RIGHT": return XInputGamepad.BTN_DPAD_RIGHT;
                default: return 0;
            }
        }

        public static bool Known(string name)
        {
            if (name == null) return false;
            return IsTrigger(name) || Bit(name) != 0;
        }

        /// <summary>All assignable pad names (for the settings UI).</summary>
        public static readonly string[] Names =
        {
            "A", "B", "X", "Y", "LB", "RB", "LT", "RT",
            "Start", "Back", "Guide", "L3", "R3",
            "DPad_Up", "DPad_Down", "DPad_Left", "DPad_Right"
        };
    }
}
