using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyFixQR.Models;
using KeyFixQR.Services;
using NativeMethods = KeyFixQR.Interop.NativeMethods;

namespace KeyFixQR.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly App _app;
        private HotkeyCombo _fixHotkey;
        private HotkeyCombo _qrHotkey;

        public SettingsWindow(App app)
        {
            _app = app;
            InitializeComponent();
            ApplyLanguage();
            LoadSettings();
        }

        private void ApplyLanguage()
        {
            FlowDirection = LocalizationService.FlowDirection;
            kbTitle.Text = LocalizationService.T("kbSectionTitle");
            kbEnable.Content = LocalizationService.T("enableKbFix");
            fixShortcutLabel.Text = LocalizationService.T("fixShortcutLabel");
            qrTitle.Text = LocalizationService.T("qrSectionTitle");
            qrEnable.Content = LocalizationService.T("enableQr");
            qrShortcutLabel.Text = LocalizationService.T("qrShortcutLabel");
            generalTitle.Text = LocalizationService.T("appearanceLabel");
            startupCheck.Content = LocalizationService.T("startupLabel");
            themeLabel.Text = LocalizationService.T("themeLabel");
            languageLabel.Text = LocalizationService.T("languageLabel");
            privacyNote.Text = LocalizationService.T("privacyNote");
            hintText.Text = LocalizationService.T("shortcutHint");
            saveBtn.Content = LocalizationService.T("saveBtn");
            cancelBtn.Content = LocalizationService.T("cancelBtn");

            dirItemAuto.Content = LocalizationService.T("dirAuto");
            dirItemEn2Fa.Content = LocalizationService.T("dirEn2Fa");
            dirItemFa2En.Content = LocalizationService.T("dirFa2En");
            themeItemLight.Content = LocalizationService.T("themeLight");
            themeItemDark.Content = LocalizationService.T("themeDark");
            themeItemAuto.Content = LocalizationService.T("themeAuto");
            langItemFa.Content = "فارسی";
            langItemEn.Content = "English";
        }

        private void LoadSettings()
        {
            var s = _app.Settings;
            _fixHotkey = new HotkeyCombo(s.KeyboardFixHotkey.Modifiers, s.KeyboardFixHotkey.VirtualKey);
            _qrHotkey = new HotkeyCombo(s.QrHotkey.Modifiers, s.QrHotkey.VirtualKey);
            fixShortcutBox.Text = _fixHotkey.ToString();
            qrShortcutBox.Text = _qrHotkey.ToString();
            kbEnable.IsChecked = s.KeyboardFixEnabled;
            qrEnable.IsChecked = s.QrEnabled;
            directionCombo.SelectedIndex = (int)s.DirectionMode;
            startupCheck.IsChecked = StartupService.IsSet() || s.StartWithWindows;
            themeCombo.SelectedIndex = s.Theme switch { "Dark" => 1, "Auto" => 2, _ => 0 };
            languageCombo.SelectedIndex = s.Language == "En" ? 1 : 0;
        }

        private void ShortcutBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
        }

        private void ShortcutBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.Escape)
                return;

            var mods = Keyboard.Modifiers;
            uint m =
                ((mods & ModifierKeys.Control) != 0 ? NativeMethods.MOD_CONTROL : 0) |
                ((mods & ModifierKeys.Alt) != 0 ? NativeMethods.MOD_ALT : 0) |
                ((mods & ModifierKeys.Shift) != 0 ? NativeMethods.MOD_SHIFT : 0) |
                ((mods & ModifierKeys.Windows) != 0 ? NativeMethods.MOD_WIN : 0);

            Key effective = e.Key == Key.System ? e.SystemKey : e.Key;
            int vk;
            try { vk = KeyInterop.VirtualKeyFromKey(effective); }
            catch { return; }
            if (vk <= 0) return;

            var combo = new HotkeyCombo(m, vk);
            if (sender == fixShortcutBox)
            {
                _fixHotkey = combo;
                fixShortcutBox.Text = combo.ToString();
            }
            else if (sender == qrShortcutBox)
            {
                _qrHotkey = combo;
                qrShortcutBox.Text = combo.ToString();
            }
            statusText.Text = "";
        }

        private void DirectionCombo_Changed(object sender, SelectionChangedEventArgs e) { }

        private void LanguageCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            string lang = languageCombo.SelectedIndex == 1 ? "En" : "Fa";
            if (lang != LocalizationService.CurrentLanguage)
            {
                LocalizationService.SetLanguage(lang);
                ApplyLanguage();
            }
        }

        private bool Validate()
        {
            if (!_fixHotkey.IsValid || !_qrHotkey.IsValid)
            {
                statusText.Text = LocalizationService.T("invalidCombo");
                return false;
            }
            if (_fixHotkey.Equals(_qrHotkey))
            {
                statusText.Text = LocalizationService.T("sameCombo");
                return false;
            }
            statusText.Text = "";
            return true;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            var s = _app.Settings;
            s.KeyboardFixEnabled = kbEnable.IsChecked == true;
            s.KeyboardFixHotkey = new HotkeyCombo(_fixHotkey.Modifiers, _fixHotkey.VirtualKey);
            s.QrEnabled = qrEnable.IsChecked == true;
            s.QrHotkey = new HotkeyCombo(_qrHotkey.Modifiers, _qrHotkey.VirtualKey);
            s.DirectionMode = (ConvertDirection)Math.Max(0, directionCombo.SelectedIndex);
            s.StartWithWindows = startupCheck.IsChecked == true;
            s.Language = languageCombo.SelectedIndex == 1 ? "En" : "Fa";
            s.Theme = themeCombo.SelectedIndex switch { 1 => "Dark", 2 => "Auto", _ => "Light" };

            SettingsService.Save(s);
            _app.ApplyStartupRegistration();
            ThemeService.Apply(s.Theme);
            _app.RegisterHotkeys();
            _app.RefreshTray();
            _app.Tray.Balloon(LocalizationService.T("savedMsg"));
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            LocalizationService.SetLanguage(_app.Settings.Language);
            Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            LocalizationService.SetLanguage(_app.Settings.Language);
            Close();
        }
    }
}
