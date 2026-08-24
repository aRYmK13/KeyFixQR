using System;
using System.Collections.Generic;
using System.Windows;

namespace KeyFixQR.Services
{
    public static class LocalizationService
    {
        private static readonly Dictionary<string, string> Fa = new()
        {
            ["activatedMsg"] = "KeyFix QR فعال شد",
            ["fixedMsg"] = "متن اصلاح شد",
            ["nothingChangedMsg"] = "تغییری لازم نبود",
            ["noSelectionMsg"] = "ابتدا متن موردنظر را انتخاب کنید.",
            ["replaceFailedMsg"] = "انجام عملیات ممکن نشد.",
            ["copiedFallbackMsg"] = "امکان جایگزینی مستقیم وجود ندارد؛ متن اصلاح‌شده در Clipboard کپی شد.",
            ["hotkeyConflict"] = "ثبت میانبر ناموفق بود؛ میانبر دیگری در تنظیمات انتخاب کنید.",
            ["qrFailed"] = "خطا در تولید QR",
            ["qrTooLong"] = "متن برای QR بیش از حد طولانی است.",
            ["copiedMsg"] = "کپی شد",
            ["copyBtn"] = "کپی متن",
            ["trayKeyboardFix"] = "اصلاح صفحه‌کلید",
            ["enabledSuffix"] = "✓ فعال",
            ["disabledSuffix"] = "✕ غیرفعال",
            ["trayQrGenerator"] = "تولید QR Code",
            ["traySettings"] = "تنظیمات",
            ["trayShortcuts"] = "میانبرهای صفحه‌کلید",
            ["trayStartWithWindows"] = "شروع با ویندوز",
            ["trayPause"] = "توقف موقت",
            ["trayResume"] = "ادامه",
            ["trayExit"] = "خروج",
            ["kbSectionTitle"] = "اصلاح صفحه‌کلید",
            ["enableKbFix"] = "فعال بودن اصلاح چیدمان صفحه‌کلید",
            ["fixShortcutLabel"] = "میانبر تبدیل متن:",
            ["directionLabel"] = "جهت تبدیل:",
            ["dirAuto"] = "تشخیص خودکار",
            ["dirEn2Fa"] = "انگلیسی ← فارسی",
            ["dirFa2En"] = "فارسی ← انگلیسی",
            ["qrSectionTitle"] = "تولید QR Code",
            ["enableQr"] = "فعال بودن تولید QR",
            ["qrShortcutLabel"] = "میانبر QR:",
            ["startupLabel"] = "اجرای خودکار با شروع ویندوز",
            ["appearanceLabel"] = "عمومی و ظاهر",
            ["themeLabel"] = "پوسته:",
            ["themeLight"] = "روشن",
            ["themeDark"] = "تیره",
            ["themeAuto"] = "هماهنگ با ویندوز",
            ["languageLabel"] = "زبان رابط:",
            ["langFa"] = "فارسی",
            ["saveBtn"] = "ذخیره",
            ["cancelBtn"] = "انصراف",
            ["shortcutHint"] = "روی کادر کلیک کنید و کلیدهای میانبر را فشار دهید",
            ["invalidCombo"] = "میانبر باید شامل Ctrl یا Alt یا Win باشد.",
            ["sameCombo"] = "میانبرهای دو قابلیت نباید یکسان باشند.",
            ["savedMsg"] = "ذخیره شد",
            ["privacyNote"] = "تمام پردازش‌ها به‌صورت محلی انجام می‌شود؛ هیچ متنی به اینترنت ارسال یا ذخیره نمی‌شود.",
            ["pausedMsg"] = "KeyFix QR موقتاً متوقف شد",
            ["resumedMsg"] = "KeyFix QR ادامه یافت"
        };

        private static readonly Dictionary<string, string> En = new()
        {
            ["activatedMsg"] = "KeyFix QR activated",
            ["fixedMsg"] = "Text fixed",
            ["nothingChangedMsg"] = "Nothing to change",
            ["noSelectionMsg"] = "Please select some text first.",
            ["replaceFailedMsg"] = "The operation could not be completed.",
            ["copiedFallbackMsg"] = "Direct replacement unavailable; fixed text was copied to the clipboard.",
            ["hotkeyConflict"] = "Hotkey registration failed; pick another shortcut in Settings.",
            ["qrFailed"] = "QR generation failed",
            ["qrTooLong"] = "Text is too long for a QR code.",
            ["copiedMsg"] = "Copied",
            ["copyBtn"] = "Copy text",
            ["trayKeyboardFix"] = "Keyboard Fix",
            ["enabledSuffix"] = "✓ Enabled",
            ["disabledSuffix"] = "✕ Disabled",
            ["trayQrGenerator"] = "QR Generator",
            ["traySettings"] = "Settings",
            ["trayShortcuts"] = "Keyboard Shortcuts",
            ["trayStartWithWindows"] = "Start with Windows",
            ["trayPause"] = "Pause",
            ["trayResume"] = "Resume",
            ["trayExit"] = "Exit",
            ["kbSectionTitle"] = "Keyboard Fix",
            ["enableKbFix"] = "Enable keyboard layout correction",
            ["fixShortcutLabel"] = "Convert shortcut:",
            ["directionLabel"] = "Conversion direction:",
            ["dirAuto"] = "Automatic detection",
            ["dirEn2Fa"] = "English → Persian",
            ["dirFa2En"] = "Persian → English",
            ["qrSectionTitle"] = "QR Generator",
            ["enableQr"] = "Enable QR generator",
            ["qrShortcutLabel"] = "QR shortcut:",
            ["startupLabel"] = "Start KeyFix QR with Windows",
            ["appearanceLabel"] = "General & Appearance",
            ["themeLabel"] = "Theme:",
            ["themeLight"] = "Light",
            ["themeDark"] = "Dark",
            ["themeAuto"] = "Follow Windows",
            ["languageLabel"] = "UI language:",
            ["langFa"] = "فارسی",
            ["saveBtn"] = "Save",
            ["cancelBtn"] = "Cancel",
            ["shortcutHint"] = "Click a box, then press the key combination you want",
            ["invalidCombo"] = "Shortcut must include Ctrl, Alt or Win.",
            ["sameCombo"] = "The two shortcuts must be different.",
            ["savedMsg"] = "Saved",
            ["privacyNote"] = "Everything is processed locally; no text is ever uploaded or stored.",
            ["pausedMsg"] = "KeyFix QR paused",
            ["resumedMsg"] = "KeyFix QR resumed"
        };

        public static string CurrentLanguage { get; private set; } = "Fa";

        public static event Action? LanguageChanged;

        public static void SetLanguage(string lang)
        {
            CurrentLanguage = lang == "En" ? "En" : "Fa";
            LanguageChanged?.Invoke();
        }

        public static string T(string key)
        {
            var dict = CurrentLanguage == "En" ? En : Fa;
            return dict.TryGetValue(key, out var v) ? v : key;
        }

        public static System.Windows.FlowDirection FlowDirection => CurrentLanguage == "Fa" ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
    }
}
