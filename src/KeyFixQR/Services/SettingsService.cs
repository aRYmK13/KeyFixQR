using System;
using System.IO;
using System.Text.Json;
using KeyFixQR.Interop;
using KeyFixQR.Models;

namespace KeyFixQR.Services
{
    public static class SettingsService
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public static string FilePath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeyFixQR");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "settings.json");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
            }
            catch (Exception ex) { NativeMethods.Log("settings load: " + ex.Message); }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
            }
            catch (Exception ex) { NativeMethods.Log("settings save: " + ex.Message); }
        }
    }

    public static class StartupService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "KeyFixQR";

        public static void Set(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null) return;
                if (enable)
                {
                    string exe = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(ValueName, "\"" + exe + "\"");
                }
                else key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            catch (Exception ex) { NativeMethods.Log("startup: " + ex.Message); }
        }

        public static bool IsSet()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(ValueName) != null;
            }
            catch { return false; }
        }
    }
}
