using System;
using System.Windows.Forms;
using KeyFixQR.Interop;

namespace KeyFixQR.Services
{
    public sealed class TrayHost : IDisposable
    {
        private readonly App _app;
        private NotifyIcon _icon;
        private ContextMenuStrip? _menu;

        public TrayHost(App app)
        {
            _app = app;
            _icon = new NotifyIcon
            {
                Icon = LoadIcon(),
                Text = "KeyFix QR",
                Visible = true
            };
            _icon.DoubleClick += (_, _) => _app.OpenSettings();
            BuildMenu();
            LocalizationService.LanguageChanged += BuildMenu;
        }

        private static System.Drawing.Icon LoadIcon()
        {
            try
            {
                using var stream = System.Windows.Application.GetResourceStream(
                    new Uri("pack://application:,,,/Resources/AppIcon.ico"))?.Stream;
                if (stream != null) return new System.Drawing.Icon(stream);
            }
            catch { }
            return System.Drawing.SystemIcons.Application;
        }

        private ToolStripItem AddItem(ContextMenuStrip menu, string text, EventHandler onClick, bool checkable = false, bool checkState = false)
        {
            var item = new ToolStripMenuItem(text)
            {
                CheckOnClick = checkable,
                Checked = checkState
            };
            item.Click += onClick;
            menu.Items.Add(item);
            return item;
        }

        public void Rebuild() => BuildMenu();

        private void BuildMenu()
        {
            try
            {
                _menu?.Dispose();
            }
            catch { }
            _menu = null;

            var s = _app.Settings;
            var menu = new ContextMenuStrip();
            menu.RightToLeft = LocalizationService.FlowDirection == System.Windows.FlowDirection.RightToLeft
                ? System.Windows.Forms.RightToLeft.Yes : System.Windows.Forms.RightToLeft.No;

            var header = new ToolStripMenuItem("KeyFix QR") { Enabled = false };
            menu.Items.Add(header);
            menu.Items.Add(new ToolStripSeparator());

            string fixText = LocalizationService.T("trayKeyboardFix") + "  " +
                (s.KeyboardFixEnabled && !s.Paused ? LocalizationService.T("enabledSuffix") : LocalizationService.T("disabledSuffix"));
            AddItem(menu, fixText, (_, _) =>
            {
                s.KeyboardFixEnabled = !s.KeyboardFixEnabled;
                PersistAndRefresh();
            });

            string qrText = LocalizationService.T("trayQrGenerator") + "  " +
                (s.QrEnabled && !s.Paused ? LocalizationService.T("enabledSuffix") : LocalizationService.T("disabledSuffix"));
            AddItem(menu, qrText, (_, _) =>
            {
                s.QrEnabled = !s.QrEnabled;
                PersistAndRefresh();
            });

            AddItem(menu, LocalizationService.T("traySettings"), (_, _) => _app.OpenSettings());
            AddItem(menu, LocalizationService.T("trayShortcuts"), (_, _) => _app.OpenSettings());

            AddItem(menu, LocalizationService.T("trayStartWithWindows"), (_, _) =>
            {
                s.StartWithWindows = !s.StartWithWindows;
                PersistAndRefresh();
            }, checkable: true, checkState: StartupService.IsSet());

            AddItem(menu,
                s.Paused ? LocalizationService.T("trayResume") : LocalizationService.T("trayPause"),
                (_, _) =>
                {
                    s.Paused = !s.Paused;
                    PersistAndRefresh();
                    Balloon(LocalizationService.T(s.Paused ? "pausedMsg" : "resumedMsg"));
                });

            menu.Items.Add(new ToolStripSeparator());
            AddItem(menu, LocalizationService.T("trayExit"), (_, _) =>
            {
                _app.CloseQr();
                System.Windows.Application.Current.Shutdown();
            });

            _icon.ContextMenuStrip = menu;
            _menu = menu;
        }

        private void PersistAndRefresh()
        {
            SettingsService.Save(_app.Settings);
            _app.ApplyStartupRegistration();
            _app.RegisterHotkeys();
            BuildMenu();
        }

        public void Balloon(string message)
        {
            try
            {
                _icon.BalloonTipTitle = "KeyFix QR";
                _icon.BalloonTipText = message;
                _icon.ShowBalloonTip(1500);
            }
            catch (Exception ex) { NativeMethods.Log("balloon: " + ex.Message); }
        }

        public void Dispose()
        {
            LocalizationService.LanguageChanged -= BuildMenu;
            _icon.Visible = false;
            _icon.Dispose();
            _menu?.Dispose();
        }
    }
}
