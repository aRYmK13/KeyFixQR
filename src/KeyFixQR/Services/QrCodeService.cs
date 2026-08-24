using System;
using QRCoder;

namespace KeyFixQR.Services
{
    public static class QrCodeService
    {
        public const bool DarkInkDark = true;
        public const bool DarkInkLight = false;

        public static byte[] GeneratePng(string text, bool useDarkCardColors)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("empty");

            using var gen = new QRCodeGenerator();
            var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.M, eciMode: QRCodeGenerator.EciMode.Utf8);
            var qr = new PngByteQRCode(data);

            byte[] dark = { 17, 24, 39, 255 };
            byte[] light = { 255, 255, 255, 255 };
            return qr.GetGraphic(8, dark, light);
        }
    }
}
