using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using KeyFixQR.Interop;

namespace KeyFixQR.Services
{
    public static class InputSender
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL, wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private static void Send(ushort vk, ushort scan, bool up)
        {
            var input = new INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD
            };
            input.U.ki.wVk = vk;
            input.U.ki.wScan = scan;
            input.U.ki.dwFlags = up ? NativeMethods.KEYEVENTF_KEYUP : 0;
            SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
        }

        private static async Task WaitReleasedAsync(int vk)
        {
            for (int i = 0; i < 40; i++)
            {
                if ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) == 0) return;
                await Task.Delay(25);
            }
        }

        public static async Task WaitModifiersReleasedAsync(params ushort[] vks)
        {
            foreach (int vk in vks) await WaitReleasedAsync(vk);
            await Task.Delay(30);
        }

        private static void Combo(ushort vk, ushort scan)
        {
            Send(NativeMethods.VK_LCONTROL, 0x1D, false);
            Send(vk, scan, false);
            Send(vk, scan, true);
            Send(NativeMethods.VK_LCONTROL, 0x1D, true);
        }

        public static void SendCtrlC() => Combo(NativeMethods.VK_C, 0x2E);

        public static void SendCtrlV() => Combo(NativeMethods.VK_V, 0x2F);
    }

    public static class MouseHelper
    {
        public static System.Windows.Point GetMousePx()
        {
            NativeMethods.GetCursorPos(out NativeMethods.POINT p);
            return new System.Windows.Point(p.X, p.Y);
        }
    }

    public static class CaretHelper
    {
        public static System.Windows.Point? GetCaretScreenPx()
        {
            try
            {
                IntPtr fg = NativeMethods.GetForegroundWindow();
                if (fg == IntPtr.Zero) return null;
                uint threadId = NativeMethods.GetWindowThreadProcessId(fg, out _);
                var info = new NativeMethods.GUITHREADINFO();
                info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GUITHREADINFO>();
                if (!NativeMethods.GetGUIThreadInfo(threadId, ref info)) return null;
                if (info.hwndCaret == IntPtr.Zero) return null;
                var pt = new NativeMethods.POINT { X = info.rcCaret.Left, Y = info.rcCaret.Bottom };
                if (!NativeMethods.ClientToScreen(info.hwndCaret, ref pt)) return null;
                return new System.Windows.Point(pt.X, pt.Y);
            }
            catch
            {
                return null;
            }
        }
    }

    public static class MonitorHelper
    {
        public static double DpiScaleAt(double px, double py)
        {
            try
            {
                var pt = new NativeMethods.POINT((int)px, (int)py);
                IntPtr hmon = NativeMethods.MonitorFromPoint(pt, 2);
                if (hmon != IntPtr.Zero &&
                    NativeMethods.GetDpiForMonitor(hmon, 0, out uint dx, out _) == 0 && dx > 0)
                    return dx / 96.0;
            }
            catch { }
            return 1.0;
        }

        public static NativeMethods.RECT WorkAreaAt(double px, double py)
        {
            var pt = new NativeMethods.POINT((int)px, (int)py);
            IntPtr hmon = NativeMethods.MonitorFromPoint(pt, 2);
            var mi = new NativeMethods.MONITORINFO();
            mi.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>();
            if (hmon != IntPtr.Zero && NativeMethods.GetMonitorInfo(hmon, ref mi))
                return mi.rcWork;
            return new NativeMethods.RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1040 };
        }
    }
}
