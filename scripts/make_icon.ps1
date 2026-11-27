param(
    [Parameter(Mandatory=$true)][string]$SourceImage,
    [Parameter(Mandatory=$true)][string]$OutputIco,
    [string]$PreviewPng = ""
)

$ErrorActionPreference = "Stop"

$csharp = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class IconBuilder
{
    public static void BuildMultiResIcon(string sourcePath, string outputIcoPath, string previewPngPath)
    {
        int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };
        List<int> frameSizes = new List<int>();
        List<byte[]> frameBytes = new List<byte[]>();

        using (Image src = Image.FromFile(sourcePath))
        {
            foreach (int sz in sizes)
            {
                using (Bitmap bmp = CreateRoundedSquareBitmap(src, sz))
                {
                    if (sz == 256 && !string.IsNullOrEmpty(previewPngPath))
                    {
                        bmp.Save(previewPngPath, ImageFormat.Png);
                    }

                    byte[] data = (sz >= 128) ? EncodePng(bmp) : EncodeIcoDib(bmp);
                    frameSizes.Add(sz);
                    frameBytes.Add(data);
                }
            }
        }

        string outDir = Path.GetDirectoryName(outputIcoPath);
        if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        using (FileStream fs = new FileStream(outputIcoPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            // ICONDIR (6 bytes)
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)frameSizes.Count);

            int offset = 6 + (16 * frameSizes.Count);
            for (int i = 0; i < frameSizes.Count; i++)
            {
                int sz = frameSizes[i];
                byte dim = (sz >= 256) ? (byte)0 : (byte)sz;
                bw.Write(dim);                     // bWidth
                bw.Write(dim);                     // bHeight
                bw.Write((byte)0);                 // bColorCount
                bw.Write((byte)0);                 // bReserved
                bw.Write((ushort)1);               // wPlanes
                bw.Write((ushort)32);              // wBitCount
                bw.Write((uint)frameBytes[i].Length); // dwBytesInRes
                bw.Write((uint)offset);            // dwImageOffset
                offset += frameBytes[i].Length;
            }

            for (int i = 0; i < frameBytes.Count; i++)
            {
                bw.Write(frameBytes[i]);
            }
        }
    }

    private static Bitmap CreateRoundedSquareBitmap(Image src, int size)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.Clear(Color.Transparent);

            float margin = (size >= 48) ? (size * 0.035f) : 0f;
            float drawSize = size - margin * 2f;
            float radius = drawSize * 0.22f;
            float diam = radius * 2f;
            float x = margin;
            float y = margin;

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(x, y, diam, diam, 180, 90);
                path.AddArc(x + drawSize - diam, y, diam, diam, 270, 90);
                path.AddArc(x + drawSize - diam, y + drawSize - diam, diam, diam, 0, 90);
                path.AddArc(x, y + drawSize - diam, diam, diam, 90, 90);
                path.CloseFigure();

                g.SetClip(path);
                g.DrawImage(src, new RectangleF(x, y, drawSize, drawSize), new RectangleF(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);

                g.ResetClip();
                if (size >= 32)
                {
                    float penWidth = Math.Max(1.0f, size / 128.0f);
                    using (Pen pen = new Pen(Color.FromArgb(78, 175, 230, 255), penWidth))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }
        return bmp;
    }

    private static byte[] EncodePng(Bitmap bmp)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    private static byte[] EncodeIcoDib(Bitmap bmp)
    {
        int w = bmp.Width;
        int h = bmp.Height;

        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            // BITMAPINFOHEADER (40 bytes)
            bw.Write((uint)40);
            bw.Write(w);
            bw.Write(h * 2); // XOR + AND height
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write((uint)0); // BI_RGB
            bw.Write((uint)(w * h * 4));
            bw.Write(0);
            bw.Write(0);
            bw.Write((uint)0);
            bw.Write((uint)0);

            Rectangle rect = new Rectangle(0, 0, w, h);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[stride * h];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);

            for (int row = h - 1; row >= 0; row--)
            {
                bw.Write(pixels, row * stride, w * 4);
            }

            // AND mask: 1bpp bottom-up, row padded to 4-byte boundary
            int andRowBytes = ((w + 31) / 32) * 4;
            byte[] andMask = new byte[andRowBytes * h];
            bw.Write(andMask);

            bw.Flush();
            return ms.ToArray();
        }
    }
}
"@

Add-Type -TypeDefinition $csharp -ReferencedAssemblies System.Drawing
[IconBuilder]::BuildMultiResIcon($SourceImage, $OutputIco, $PreviewPng)
Write-Host "ICO 生成成功: $OutputIco (16, 24, 32, 48, 64, 128, 256)" -ForegroundColor Green
