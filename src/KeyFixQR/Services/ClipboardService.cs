using System;
using System.Windows;
using KeyFixQR.Interop;

namespace KeyFixQR.Services
{
    public struct ClipboardBackup
    {
        public string? Text;
        public bool HadAny;
    }

    public sealed class GlobalHotkeyService : IDisposable
    {
        private System.Windows.Interop.HwndSource? _source;
        private readonly System.Collections.Generic.Dictionary<int, Action> _callbacks = new();

        public event Action<int>? RegistrationFailed;

        private IntPtr EnsureWindow()
        {
            if (_source != null) return _source.Handle;
            var parms = new System.Windows.Interop.HwndSourceParameters("KeyFixQRMsgWindow")
            {
                PositionX = 0,
                PositionY = 0,
                Width = 0,
                Height = 0,
                ParentWindow = new IntPtr(-3),
                WindowStyle = 0
            };
            _source = new System.Windows.Interop.HwndSource(parms);
            _source.AddHook(WndProc);
            return _source.Handle;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_callbacks.TryGetValue(id, out var cb))
                {
                    handled = true;
                    cb();
                }
            }
            return IntPtr.Zero;
        }

        public bool Register(int id, Models.HotkeyCombo combo, Action callback)
        {
            Unregister(id);
            IntPtr hwnd = EnsureWindow();
            uint mods = (combo.Modifiers & (NativeMethods.MOD_ALT | NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_WIN)) | NativeMethods.MOD_NOREPEAT;
            bool ok = NativeMethods.RegisterHotKey(hwnd, id, mods, (uint)combo.VirtualKey);
            if (ok) _callbacks[id] = callback;
            else RegistrationFailed?.Invoke(id);
            return ok;
        }

        public void Unregister(int id)
        {
            if (_callbacks.Remove(id) && _source != null)
                NativeMethods.UnregisterHotKey(_source.Handle, id);
        }

        public void Dispose()
        {
            foreach (int id in new System.Collections.Generic.List<int>(_callbacks.Keys))
                Unregister(id);
            _source?.Dispose();
            _source = null;
        }
    }

    public sealed class ClipboardService
    {
        private const int MaxRetries = 15;

        public string? GetTextSafe()
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
                catch (System.Runtime.InteropServices.COMException) { System.Threading.Thread.Sleep(30); }
            }
            return null;
        }

        public bool SetTextSafe(string text)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try { Clipboard.SetText(text); return true; }
                catch (System.Runtime.InteropServices.COMException) { System.Threading.Thread.Sleep(30); }
            }
            return false;
        }

        public void ClearSafe()
        {
            try { Clipboard.Clear(); }
            catch (System.Runtime.InteropServices.COMException) { }
        }

        public async System.Threading.Tasks.Task<string?> WaitForTextAsync(int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                string? t = GetTextSafe();
                if (!string.IsNullOrEmpty(t)) return t;
                await System.Threading.Tasks.Task.Delay(35);
            }
            return GetTextSafe();
        }

        public ClipboardBackup Backup()
        {
            string? t = null;
            try { if (Clipboard.ContainsText()) t = Clipboard.GetText(); } catch { }
            bool any = !string.IsNullOrEmpty(t);
            try { any |= Clipboard.ContainsImage() || Clipboard.ContainsFileDropList() || Clipboard.ContainsAudio(); } catch { }
            return new ClipboardBackup { Text = t, HadAny = any };
        }

        public void Restore(ClipboardBackup backup)
        {
            try
            {
                if (!backup.HadAny) { ClearSafe(); return; }
                SetTextSafe(backup.Text ?? "");
            }
            catch { }
        }
    }
}
