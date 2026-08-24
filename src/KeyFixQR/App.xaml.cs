using System;
using System.Threading;
using System.Windows;
using KeyFixQR.Interop;
using KeyFixQR.Models;
using KeyFixQR.Services;
using KeyFixQR.Views;

namespace KeyFixQR
{
    public partial class App : Application
    {
        private const int HotkeyFixId = 1;
        private const int HotkeyQrId = 2;
        private static Mutex? _mutex;

        internal AppSettings Settings { get; private set; } = new();
        internal GlobalHotkeyService Hotkeys { get; private set; } = new();
        internal ClipboardService Clipboard { get; } = new();
        internal TrayHost Tray { get; private set; } = null!;

        private SettingsWindow? _settingsWindow;
        private QrOverlayWindow? _qrWindow;
        private bool _busy;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, @"Local\KeyFixQR.SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);
            DispatcherUnhandledException += (_, args) =>
            {
                NativeMethods.Log("Unhandled: " + args.Exception.Message);
                args.Handled = true;
            };

            Settings = SettingsService.Load();
            try
            {
                ThemeService.Apply(Settings.Theme);
                LocalizationService.SetLanguage(Settings.Language);

                Tray = new TrayHost(this);
                Tray.Rebuild();

                ApplyStartupRegistration();
                RegisterHotkeys();
                Tray.Balloon(LocalizationService.T("activatedMsg"));
            }
            catch (Exception ex)
            {
                NativeMethods.Log("startup: " + ex);
            }

            if (e.Args.Any(a => a.Equals("--settings", StringComparison.OrdinalIgnoreCase)))
                Dispatcher.BeginInvoke(() => OpenSettings());

            if (e.Args.Any(a => a.Equals("--qr", StringComparison.OrdinalIgnoreCase)))
                Dispatcher.BeginInvoke(() =>
                {
                    byte[] png = QrCodeService.GeneratePng("https://example.com/keyfix-qr-selftest", false);
                    ShowQr(png, "https://example.com/keyfix-qr-selftest", new Point(500, 400));
                });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Hotkeys.Dispose();
            Tray.Dispose();
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }

        public void RegisterHotkeys()
        {
            Hotkeys.Unregister(HotkeyFixId);
            Hotkeys.Unregister(HotkeyQrId);
            if (Settings.Paused) return;

            if (Settings.KeyboardFixEnabled)
            {
                if (!Hotkeys.Register(HotkeyFixId, Settings.KeyboardFixHotkey, () => SafeAsync(() => RunKeyboardFixAsync())))
                    Tray.Balloon(LocalizationService.T("hotkeyConflict"));
            }
            if (Settings.QrEnabled)
            {
                if (!Hotkeys.Register(HotkeyQrId, Settings.QrHotkey, () => SafeAsync(() => RunQrAsync())))
                    Tray.Balloon(LocalizationService.T("hotkeyConflict"));
            }
        }

        public void ApplyStartupRegistration() => StartupService.Set(Settings.StartWithWindows);

        public void RefreshTray() => Tray.Rebuild();

        private void SafeAsync(Func<Task> work)
        {
            try { _ = work(); }
            catch (Exception ex) { NativeMethods.Log("hotkey: " + ex.Message); }
        }

        private async Task RunKeyboardFixAsync()
        {
            if (_busy || Settings.Paused) return;
            _busy = true;
            ClipboardBackup backup = default;
            try
            {
                await InputSender.WaitModifiersReleasedAsync(NativeMethods.VK_CONTROL, NativeMethods.VK_MENU, (ushort)Settings.KeyboardFixHotkey.VirtualKey);
                backup = Clipboard.Backup();
                Clipboard.ClearSafe();
                await Task.Delay(80);
                InputSender.SendCtrlC();
                string? selected = await Clipboard.WaitForTextAsync(800);
                if (string.IsNullOrEmpty(selected))
                {
                    await Task.Delay(150);
                    InputSender.SendCtrlC();
                    selected = await Clipboard.WaitForTextAsync(600);
                }
                if (string.IsNullOrEmpty(selected))
                {
                    Clipboard.Restore(backup);
                    Tray.Balloon(LocalizationService.T("noSelectionMsg"));
                    return;
                }

                ConvertDirection dir = Settings.DirectionMode == ConvertDirection.Auto
                    ? KeyboardLayoutService.DetectDirection(selected)
                    : Settings.DirectionMode;
                string converted = KeyboardLayoutService.Convert(selected, dir);
                NativeMethods.Log($"fix: dir={dir} len={selected.Length} changed={converted != selected}");

                if (converted == selected)
                {
                    Clipboard.Restore(backup);
                    Tray.Balloon(LocalizationService.T("nothingChangedMsg"));
                    return;
                }

                if (!Clipboard.SetTextSafe(converted))
                {
                    Clipboard.Restore(backup);
                    NativeMethods.Log("fix: SetText FAILED (clipboard locked)");
                    Tray.Balloon(LocalizationService.T("replaceFailedMsg"));
                    return;
                }

                uint seq = NativeMethods.GetClipboardSequenceNumber();
                await Task.Delay(150);
                InputSender.SendCtrlV();
                await Task.Delay(700);
                if (NativeMethods.GetClipboardSequenceNumber() == seq)
                    Clipboard.Restore(backup);
                else
                    NativeMethods.Log("fix: skip restore (clipboard changed externally)");
                Tray.Balloon(LocalizationService.T("fixedMsg"));
            }
            catch (Exception ex)
            {
                try { Clipboard.Restore(backup); } catch { }
                NativeMethods.Log("fix: " + ex.Message);
                Tray.Balloon(LocalizationService.T("replaceFailedMsg"));
            }
            finally { _busy = false; }
        }

        private async Task RunQrAsync()
        {
            if (Settings.Paused || _busy) return;
            _busy = true;
            ClipboardBackup backup = default;
            try
            {
                if (_qrWindow != null && _qrWindow.IsVisible)
                {
                    await InputSender.WaitModifiersReleasedAsync(NativeMethods.VK_CONTROL, NativeMethods.VK_MENU, (ushort)Settings.QrHotkey.VirtualKey);
                    backup = Clipboard.Backup();
                    Clipboard.ClearSafe();
                    await Task.Delay(60);
                    InputSender.SendCtrlC();
                    string? sel = await Clipboard.WaitForTextAsync(600);
                    Clipboard.Restore(backup);
                    if (!string.IsNullOrEmpty(sel) && sel != _qrWindow.Payload)
                    {
                        UpdateQrContent(sel);
                        Point caret = CaretHelper.GetCaretScreenPx() ?? MouseHelper.GetMousePx();
                        _qrWindow.PlaceNear(caret);
                        _qrWindow.Activate();
                    }
                    else
                    {
                        CloseQr();
                    }
                    return;
                }

                await InputSender.WaitModifiersReleasedAsync(NativeMethods.VK_CONTROL, NativeMethods.VK_MENU, (ushort)Settings.QrHotkey.VirtualKey);
                backup = Clipboard.Backup();
                Clipboard.ClearSafe();
                await Task.Delay(60);
                InputSender.SendCtrlC();
                string? text = await Clipboard.WaitForTextAsync(700);
                if (string.IsNullOrEmpty(text))
                {
                    Clipboard.Restore(backup);
                    Tray.Balloon(LocalizationService.T("noSelectionMsg"));
                    return;
                }

                byte[] png;
                try
                {
                    png = QrCodeService.GeneratePng(text, ThemeService.IsDark ? QrCodeService.DarkInkDark : QrCodeService.DarkInkLight);
                }
                catch (Exception qex)
                {
                    Clipboard.Restore(backup);
                    Tray.Balloon(qex.Message.Length > 90 ? LocalizationService.T("qrTooLong") : LocalizationService.T("qrFailed"));
                    return;
                }

                Point anchor = CaretHelper.GetCaretScreenPx() ?? MouseHelper.GetMousePx();
                if (Clipboard.SetTextSafe(text))
                {
                    uint seq = NativeMethods.GetClipboardSequenceNumber();
                    await Task.Delay(400);
                    if (NativeMethods.GetClipboardSequenceNumber() == seq)
                        Clipboard.Restore(backup);
                }
                else Clipboard.Restore(backup);

                ShowQr(png, text, anchor);
            }
            catch (Exception ex)
            {
                try { Clipboard.Restore(backup); } catch { }
                NativeMethods.Log("qr: " + ex.Message);
                Tray.Balloon(LocalizationService.T("qrFailed"));
            }
            finally { _busy = false; }
        }

        private void ShowQr(byte[] png, string payload, Point anchorPx)
        {
            CloseQr();
            _qrWindow = new QrOverlayWindow
            {
                CopyRequested = s => Clipboard.SetTextSafe(s)
            };
            _qrWindow.SetImage(png, payload);
            _qrWindow.PlaceNear(anchorPx);
            _qrWindow.Closed += (_, _) => _qrWindow = null;
            _qrWindow.Show();
        }

        private void UpdateQrContent(string newText)
        {
            if (_qrWindow == null) return;
            byte[] png = QrCodeService.GeneratePng(newText, ThemeService.IsDark ? QrCodeService.DarkInkDark : QrCodeService.DarkInkLight);
            _qrWindow.SetImage(png, newText);
        }

        public void CloseQr()
        {
            var w = _qrWindow;
            _qrWindow = null;
            w?.Close();
        }

        public void OpenSettings()
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }
    }
}
