using KeyFixQR.Models;
using KeyFixQR.Services;
using Xunit;

namespace KeyFixQR.Tests
{
    public class KeyboardLayoutTests
    {
        [Theory]
        [InlineData("hello world", "\u0627\u062B\u0645\u0645\u062E \u0635\u062E\u0642\u0645\u06CC")]
        [InlineData("sjhf l;d", "\u0633\u062A\u0627\u0628 \u0645\u06A9\u06CC")]
        [InlineData("qwerty", "\u0636\u0635\u062B\u0642\u0641\u063A")]
        public void EnglishToPersian_MapsExactly(string input, string expected)
        {
            string result = KeyboardLayoutService.Convert(input, ConvertDirection.EnglishToPersian);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void PersianToEnglish_ReversesHelloWorld()
        {
            string fa = KeyboardLayoutService.Convert("hello world", ConvertDirection.EnglishToPersian);
            string back = KeyboardLayoutService.Convert(fa, ConvertDirection.PersianToEnglish);
            Assert.Equal("hello world", back);
        }

        [Fact]
        public void PersianPeh_IsOnBackslashKey()
        {
            Assert.Equal("\u067E", KeyboardLayoutService.Convert("\\", ConvertDirection.EnglishToPersian));
            Assert.Equal("\\", KeyboardLayoutService.Convert("\u067E", ConvertDirection.PersianToEnglish));
        }

        [Fact]
        public void DigitsAndUnmappedCharsArePreserved()
        {
            Assert.Equal("1234567890-_=+", KeyboardLayoutService.Convert("1234567890-_=+", ConvertDirection.EnglishToPersian));
            Assert.Equal("\u0637#\u063A", KeyboardLayoutService.Convert("x#y", ConvertDirection.EnglishToPersian));
            Assert.Equal("#", KeyboardLayoutService.Convert("#", ConvertDirection.EnglishToPersian));
        }

        [Fact]
        public void WhitespacePreserved_MultiLine()
        {
            string input = "hi\r\n\tyou\tme ";
            string converted = KeyboardLayoutService.Convert(input, ConvertDirection.EnglishToPersian);
            Assert.Contains("\r\n", converted);
            Assert.Contains("\t", converted);
            Assert.EndsWith(" ", converted);
        }

        [Fact]
        public void RoundTrip_AllMappedPairs()
        {
            foreach (var row in KeyboardLayoutServiceTestAccessor.GetRows())
            {
                if (row.FaNormal != row.EnBase)
                {
                    Assert.Equal(row.EnBase.ToString(), KeyboardLayoutService.Convert(row.FaNormal.ToString(), ConvertDirection.PersianToEnglish));
                }
                Assert.Equal(row.FaNormal.ToString(), KeyboardLayoutService.Convert(row.EnBase.ToString(), ConvertDirection.EnglishToPersian));

                if (row.FaShift != row.EnShift)
                    Assert.Equal(row.EnShift.ToString(), KeyboardLayoutService.Convert(row.FaShift.ToString(), ConvertDirection.PersianToEnglish));
                else
                    Assert.Equal(row.EnShift.ToString(), KeyboardLayoutService.Convert(row.EnShift.ToString(), ConvertDirection.PersianToEnglish));
            }
        }

        [Theory]
        [InlineData("hello there", ConvertDirection.EnglishToPersian)]
        [InlineData("\u0633\u0644\u0627\u0645 \u062F\u0646\u06CC\u0627", ConvertDirection.PersianToEnglish)]
        [InlineData("12345", ConvertDirection.EnglishToPersian)]
        public void AutoDetect_PicksDominantScript(string text, ConvertDirection expected)
        {
            Assert.Equal(expected, KeyboardLayoutService.DetectDirection(text));
        }

        [Fact]
        public void MixedText_UsesMajority()
        {
            // mostly english
            var d1 = KeyboardLayoutService.DetectDirection("this is \u0633\u0644\u0627\u0645 mixed");
            Assert.Equal(ConvertDirection.EnglishToPersian, d1);
            // mostly persian
            var d2 = KeyboardLayoutService.DetectDirection("\u0633\u0644\u0627\u0645 this");
            Assert.Equal(ConvertDirection.PersianToEnglish, d2);
        }

        [Fact]
        public void ShiftedSymbols_MapCorrectly()
        {
            Assert.Equal("\u061F", KeyboardLayoutService.Convert("?", ConvertDirection.EnglishToPersian));
            Assert.Equal("\u060C", KeyboardLayoutService.Convert("T", ConvertDirection.EnglishToPersian));
            Assert.Equal("\u00D7", KeyboardLayoutService.Convert("~", ConvertDirection.EnglishToPersian));
            Assert.Equal("?", KeyboardLayoutService.Convert("\u061F", ConvertDirection.PersianToEnglish));
        }

        [Fact]
        public void ParenthesesAreIdentityLikeUsLayout()
        {
            Assert.Equal("(", KeyboardLayoutService.Convert("(", ConvertDirection.EnglishToPersian));
            Assert.Equal(")", KeyboardLayoutService.Convert(")", ConvertDirection.EnglishToPersian));
            Assert.Equal("(", KeyboardLayoutService.Convert("(", ConvertDirection.PersianToEnglish));
        }
    }

    public static class KeyboardLayoutServiceTestAccessor
    {
        public static (char EnBase, char EnShift, char FaNormal, char FaShift)[] GetRows()
        {
            var field = typeof(KeyboardLayoutService).GetField("Rows",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (ValueTuple<char, char, char, char>[])field!.GetValue(null)!;
        }
    }

    public class SettingsTests
    {
        [Fact]
        public void SettingsRoundTrip_KeepsValues()
        {
            var s = new AppSettings
            {
                KeyboardFixEnabled = false,
                QrEnabled = true,
                DirectionMode = ConvertDirection.PersianToEnglish,
                StartWithWindows = true,
                Theme = "Dark",
                Language = "En",
                QrHotkey = new HotkeyCombo(3, 'K'),
                Paused = false
            };
            SettingsService.Save(s);
            var loaded = SettingsService.Load();
            Assert.False(loaded.KeyboardFixEnabled);
            Assert.True(loaded.QrEnabled);
            Assert.Equal(ConvertDirection.PersianToEnglish, loaded.DirectionMode);
            Assert.True(loaded.StartWithWindows);
            Assert.Equal("Dark", loaded.Theme);
            Assert.Equal("En", loaded.Language);
            Assert.Equal((uint)3, loaded.QrHotkey.Modifiers);
            Assert.Equal('K', loaded.QrHotkey.VirtualKey);
        }

        [Fact]
        public void HotkeyCombo_DisplayAndValidity()
        {
            var combo = new HotkeyCombo(KeyFixQR.Interop.NativeMethods.MOD_CONTROL | KeyFixQR.Interop.NativeMethods.MOD_ALT, 0x20);
            Assert.Equal("Ctrl + Alt + Space", combo.ToString());
            Assert.True(combo.IsValid);
            Assert.False(new HotkeyCombo(KeyFixQR.Interop.NativeMethods.MOD_SHIFT, 0x41).IsValid);
        }
    }

    public class QrCodeTests
    {
        [Fact]
        public void GeneratesValidPng_ForShortAsciiText()
        {
            byte[] png = QrCodeService.GeneratePng("https://example.com/keyfixqr", false);
            Assert.True(png.Length > 200);
            Assert.Equal(0x89, png[0]);
            Assert.Equal((byte)'P', png[1]);
            Assert.Equal((byte)'N', png[2]);
            Assert.Equal((byte)'G', png[3]);
        }

        [Fact]
        public void GeneratesValidPng_ForPersianText()
        {
            byte[] png = QrCodeService.GeneratePng("\u0633\u0644\u0627\u0645 \u062F\u0646\u06CC\u0627 - QR \u062A\u0633\u062A", true);
            Assert.True(png.Length > 200);
            Assert.Equal(0x89, png[0]);
        }

        [Fact]
        public void GeneratesValidPng_ForMultilineMixedText()
        {
            byte[] png = QrCodeService.GeneratePng("Line1\n\u062E\u0637 2\n1234567890 https://test.ir?a=1&b=2", false);
            Assert.True(png.Length > 200);
        }

        [Fact]
        public void Throws_OnEmptyInput()
        {
            Assert.Throws<ArgumentException>(() => QrCodeService.GeneratePng("", false));
        }
    }
}
