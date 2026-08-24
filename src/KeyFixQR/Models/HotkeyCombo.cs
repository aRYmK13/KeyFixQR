using System;
using KeyFixQR.Interop;

namespace KeyFixQR.Models
{
    public enum ConvertDirection
    {
        Auto,
        EnglishToPersian,
        PersianToEnglish
    }

    public sealed class AppSettings
    {
        public bool KeyboardFixEnabled { get; set; } = true;
        public HotkeyCombo KeyboardFixHotkey { get; set; } = new(0x0006, 0x20);
        public bool QrEnabled { get; set; } = true;
        public HotkeyCombo QrHotkey { get; set; } = new(0x0006, 0x51);
        public ConvertDirection DirectionMode { get; set; } = ConvertDirection.Auto;
        public bool StartWithWindows { get; set; }
        public string Theme { get; set; } = "Auto";
        public string Language { get; set; } = "Fa";
        public bool Paused { get; set; }
    }

    public sealed class HotkeyCombo : IEquatable<HotkeyCombo>
    {
        public uint Modifiers { get; set; }
        public int VirtualKey { get; set; }

        public HotkeyCombo() { Modifiers = 0x6; VirtualKey = 0x20; }

        public HotkeyCombo(uint modifiers, int virtualKey)
        {
            Modifiers = modifiers;
            VirtualKey = virtualKey;
        }

        public bool IsValid =>
            VirtualKey != 0 &&
            (Modifiers & (NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_WIN)) != 0;

        public override string ToString()
        {
            string parts = "";
            if ((Modifiers & NativeMethods.MOD_CONTROL) != 0) parts += "Ctrl + ";
            if ((Modifiers & NativeMethods.MOD_ALT) != 0) parts += "Alt + ";
            if ((Modifiers & NativeMethods.MOD_SHIFT) != 0) parts += "Shift + ";
            if ((Modifiers & NativeMethods.MOD_WIN) != 0) parts += "Win + ";
            return parts + KeyName(VirtualKey);
        }

        private static string KeyName(int vk)
        {
            return vk switch
            {
                0x20 => "Space",
                0x0D => "Enter",
                0x09 => "Tab",
                0x2D => "Insert",
                0x2E => "Delete",
                0x24 => "Home",
                0x23 => "End",
                0x21 => "PgUp",
                0x22 => "PgDn",
                >= 0x70 and <= 0x87 => "F" + (vk - 0x6F),
                >= 0x30 and <= 0x39 => ((char)vk).ToString(),
                >= 0x41 and <= 0x5A => ((char)vk).ToString(),
                >= 0x60 and <= 0x69 => "Num " + (vk - 0x60),
                _ => vk.ToString()
            };
        }

        public bool Equals(HotkeyCombo? other) => other is not null && Modifiers == other.Modifiers && VirtualKey == other.VirtualKey;
        public override bool Equals(object? obj) => Equals(obj as HotkeyCombo);
        public override int GetHashCode() => HashCode.Combine(Modifiers, VirtualKey);
    }
}
