using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace KeyFixQR.Services
{
    public static class ThemeService
    {
        private static readonly object Lock = new();

        public static bool IsDark { get; private set; }

        public static void Apply(string themeSetting)
        {
            lock (Lock)
            {
                IsDark = themeSetting switch
                {
                    "Dark" => true,
                    "Light" => false,
                    _ => SystemPrefersLight() == false
                };

                var dict = new ResourceDictionary();
                Color bg = IsDark ? FromHex("#0F172A") : FromHex("#F3F4F6");
                Color card = IsDark ? FromHex("#1E293B") : FromHex("#FFFFFF");
                Color border = IsDark ? FromHex("#334155") : FromHex("#E5E7EB");
                Color fg = IsDark ? FromHex("#E2E8F0") : FromHex("#111827");
                Color sub = IsDark ? FromHex("#94A3B8") : FromHex("#6B7280");
                Color accent = IsDark ? FromHex("#6366F1") : FromHex("#4F46E5");
                Color hover = IsDark ? FromHex("#273548") : FromHex("#EEF2FF");

                dict["BgBrush"] = new SolidColorBrush(bg);
                dict["CardBrush"] = new SolidColorBrush(card);
                dict["BorderBrush"] = new SolidColorBrush(border);
                dict["FgBrush"] = new SolidColorBrush(fg);
                dict["SubBrush"] = new SolidColorBrush(sub);
                dict["AccentBrush"] = new SolidColorBrush(accent);
                dict["AccentFgBrush"] = new SolidColorBrush(Colors.White);
                dict["HoverBrush"] = new SolidColorBrush(hover);

                var md = Application.Current.Resources.MergedDictionaries;
                for (int i = 0; i < md.Count; i++)
                {
                    if (md[i].Contains("IsRuntimeTheme")) { md.RemoveAt(i); break; }
                }
                dict["IsRuntimeTheme"] = true;
                md.Insert(0, dict);
            }
        }

        private static bool SystemPrefersLight()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int v) return v != 0;
            }
            catch { }
            return true;
        }

        private static Color FromHex(string hex)
        {
            byte r = Convert.ToByte(hex.Substring(hex.Length - 6, 2), 16);
            byte g = Convert.ToByte(hex.Substring(hex.Length - 4, 2), 16);
            byte b = Convert.ToByte(hex.Substring(hex.Length - 2, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }
    }
}
