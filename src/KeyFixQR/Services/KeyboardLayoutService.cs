using System;
using System.Collections.Generic;
using KeyFixQR.Models;

namespace KeyFixQR.Services
{
    public static class KeyboardLayoutService
    {
        // Exact Windows "Persian" layout (KBDFA.DLL, KLID 00000429).
        // Tuple: englishBaseChar (unshifted key), englishShiftChar, persianNormal, persianShift.
        private static readonly (char EnBase, char EnShift, char FaNormal, char FaShift)[] Rows =
        {
            ('\u0060','~', '\u00F7','\u00D7'),
            ('1','!', '1','!'),
            ('2','@', '2','@'),
            ('3','#', '3','#'),
            ('4','$', '4','$'),
            ('5','%', '5','%'),
            ('6','^', '6','^'),
            ('7','&', '7','&'),
            ('8','*', '8','*'),
            ('9',')', '9',')'),
            ('0','(', '0','('),
            ('-','_', '-','_'),
            ('=','+', '=','+'),
            ('q','Q', '\u0636','\u064B'),
            ('w','W', '\u0635','\u064C'),
            ('e','E', '\u062B','\u064D'),
            ('r','R', '\u0642','\uFDFC'),
            ('t','T', '\u0641','\u060C'),
            ('y','Y', '\u063A','\u061B'),
            ('u','U', '\u0639',','),
            ('i','I', '\u0647',']'),
            ('o','O', '\u062E','['),
            ('p','P', '\u062D','\\'),
            ('[','{', '\u062C','}'),
            (']','}', '\u0686','{'),
            ('a','A', '\u0634','\u064E'),
            ('s','S', '\u0633','\u064F'),
            ('d','D', '\u06CC','\u0650'),
            ('f','F', '\u0628','\u0651'),
            ('g','G', '\u0644','\u06C0'),
            ('h','H', '\u0627','\u0622'),
            ('j','J', '\u062A','\u0640'),
            ('k','K', '\u0646','\u00AB'),
            ('l','L', '\u0645','\u00BB'),
            (';',':', '\u06A9',':'),
            ('\'','"', '\u06AF','"'),
            ('\\','|', '\u067E','|'),
            ('z','Z', '\u0638','\u0629'),
            ('x','X', '\u0637','\u064A'),
            ('c','C', '\u0632','\u0698'),
            ('v','V', '\u0631','\u0624'),
            ('b','B', '\u0630','\u0625'),
            ('n','N', '\u062F','\u0623'),
            ('m','M', '\u0626','\u0621'),
            (',','<', '\u0648','<'),
            ('.','>', '.','>'),
            ('/','?', '/','\u061F')
        };

        private static readonly Dictionary<char, char> EnToFa = new();
        private static readonly Dictionary<char, char> FaToEn = new();

        static KeyboardLayoutService()
        {
            foreach (var r in Rows)
            {
                EnToFa[r.EnBase] = r.FaNormal;
                if (!EnToFa.ContainsKey(r.EnShift))
                    EnToFa[r.EnShift] = r.FaShift;
                if (r.FaNormal != r.EnBase && !FaToEn.ContainsKey(r.FaNormal))
                    FaToEn[r.FaNormal] = r.EnBase;
                if (!FaToEn.ContainsKey(r.FaShift))
                    FaToEn[r.FaShift] = r.EnShift;
            }
        }

        public static string Convert(string text, ConvertDirection direction)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            if (direction == ConvertDirection.Auto)
                direction = DetectDirection(text);

            var map = direction == ConvertDirection.EnglishToPersian ? EnToFa : FaToEn;
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (map.TryGetValue(chars[i], out char mapped))
                    chars[i] = mapped;
            }
            return new string(chars);
        }

        public static ConvertDirection DetectDirection(string text)
        {
            int fa = 0, en = 0;
            foreach (char c in text)
            {
                if ((c >= '\u0600' && c <= '\u06FF') ||
                    (c >= '\u0750' && c <= '\u077F') ||
                    (c >= '\uFB50' && c <= '\uFDFF') ||
                    (c >= '\uFE70' && c <= '\uFEFF'))
                    fa++;
                else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    en++;
            }
            return (fa > 0 && fa >= en) ? ConvertDirection.PersianToEnglish : ConvertDirection.EnglishToPersian;
        }
    }
}
