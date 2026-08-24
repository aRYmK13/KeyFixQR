$source = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class IconGen
{
    public static byte[] DrawPng(int size)
    {
        using (Bitmap bmp = new Bitmap(size, size))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                int edge = size - 1;
                int r = Math.Max(2, (int)(size * 0.24));
                int d = r * 2;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(0, 0, d, d, 180, 90);
                    path.AddArc(edge - d, 0, d, d, 270, 90);
                    path.AddArc(edge - d, edge - d, d, d, 0, 90);
                    path.AddArc(0, edge - d, d, d, 90, 90);
                    path.CloseFigure();

                    Color c1 = Color.FromArgb(255, 79, 70, 229);
                    Color c2 = Color.FromArgb(255, 147, 51, 234);
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        new PointF(0f, 0f), new PointF(edge, edge), c1, c2))
                    {
                        g.FillPath(brush, path);
                    }

                    float cell = size * 0.30f;
                    float pad = size * 0.12f;
                    float gap = size * 0.06f;
                    float[,] positions = { { 0f, 0f }, { 1f, 0f }, { 0f, 1f } };
                    for (int i = 0; i < 3; i++)
                    {
                        float x = pad + positions[i, 0] * (cell + gap);
                        float y = pad + positions[i, 1] * (cell + gap);
                        g.FillRectangle(Brushes.White, x, y, cell, cell);
                        float innerW = cell * 0.45f;
                        float off = (cell - innerW) / 2f;
                        using (SolidBrush accent = new SolidBrush(c1))
                        {
                            g.FillRectangle(accent, x + off, y + off, innerW, innerW);
                        }
                    }

                    float ds = Math.Max(1.5f, size * 0.05f);
                    float[,] dots = {
                        { size * 0.62f, size * 0.66f },
                        { size * 0.74f, size * 0.62f },
                        { size * 0.68f, size * 0.78f },
                        { size * 0.80f, size * 0.76f }
                    };
                    for (int i = 0; i < 4; i++)
                        g.FillEllipse(Brushes.White, dots[i, 0], dots[i, 1], ds, ds);
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }

    public static void Build(string outPath, int[] sizes)
    {
        int n = sizes.Length;
        byte[][] pngs = new byte[n][];
        for (int i = 0; i < n; i++) pngs[i] = DrawPng(sizes[i]);

        using (MemoryStream outMs = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(outMs))
        {
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)n);

            int offset = 6 + 16 * n;
            for (int i = 0; i < n; i++)
            {
                byte dim = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
                bw.Write(dim);
                bw.Write(dim);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write((uint)pngs[i].Length);
                bw.Write((uint)offset);
                offset += pngs[i].Length;
            }
            for (int i = 0; i < n; i++) bw.Write(pngs[i]);
            bw.Flush();
            File.WriteAllBytes(outPath, outMs.ToArray());
            Console.WriteLine("icon written: " + outPath + " (" + outMs.Length + " bytes)");
        }
    }
}
"@

Add-Type -TypeDefinition $source -ReferencedAssemblies @("System.Drawing") -Language CSharp
$outPath = Join-Path $PSScriptRoot "..\src\KeyFixQR\Resources\AppIcon.ico"
$sizes = @(16, 24, 32, 48, 64, 128, 256)
[IconGen]::Build($outPath, $sizes)
