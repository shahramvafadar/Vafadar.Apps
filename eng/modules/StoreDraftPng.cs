using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Vafadar.Build
{
    /// <summary>Serializes opaque native render pixels as RGB PNG without resizing, compositing or changing colors.</summary>
    public static class StoreDraftPng
    {
        /// <summary>Copies each exact opaque RGB value and returns width, height and the uncompressed RGB digest.</summary>
        public static string[] Encode(string source, string destination)
        {
            using (var original = new Bitmap(source))
            using (var output = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb))
            {
                var bounds = new Rectangle(0, 0, original.Width, original.Height);
                BitmapData input = original.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    BitmapData target = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var inputRow = new byte[Math.Abs(input.Stride)];
                        var outputRow = new byte[Math.Abs(target.Stride)];
                        for (int y = 0; y < original.Height; y++)
                        {
                            Marshal.Copy(new IntPtr(input.Scan0.ToInt64() + (long)y * input.Stride), inputRow, 0, inputRow.Length);
                            for (int x = 0; x < original.Width; x++)
                            {
                                if (inputRow[4*x+3] != 255) throw new InvalidDataException("A store render has nonopaque pixels; preserve the original.");
                                outputRow[3*x] = inputRow[4*x];
                                outputRow[3*x+1] = inputRow[4*x+1];
                                outputRow[3*x+2] = inputRow[4*x+2];
                            }
                            Marshal.Copy(outputRow, 0, new IntPtr(target.Scan0.ToInt64() + (long)y * target.Stride), outputRow.Length);
                        }
                    }
                    finally { output.UnlockBits(target); }
                }
                finally { original.UnlockBits(input); }
                using (var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    output.Save(file, ImageFormat.Png);
                // Verify with the actual persisted PNG decoder instead of trusting a successful encoder return.
                using (var decoded = new Bitmap(destination))
                using (var digest = SHA256.Create())
                {
                    if (decoded.Width != original.Width || decoded.Height != original.Height)
                        throw new InvalidDataException("Store encoding changed dimensions.");
                    var pixels = new byte[checked(original.Width * original.Height * 3)];
                    int offset = 0;
                    for (int y = 0; y < original.Height; y++)
                    for (int x = 0; x < original.Width; x++)
                    {
                        Color before = original.GetPixel(x, y), after = decoded.GetPixel(x, y);
                        if (before.R != after.R || before.G != after.G || before.B != after.B)
                            throw new InvalidDataException("Store encoding changed an original RGB pixel.");
                        pixels[offset++] = before.R; pixels[offset++] = before.G; pixels[offset++] = before.B;
                    }
                    return new[] { original.Width.ToString(CultureInfo.InvariantCulture), original.Height.ToString(CultureInfo.InvariantCulture),
                        BitConverter.ToString(digest.ComputeHash(pixels)).Replace("-", "").ToLowerInvariant() };
                }
            }
        }
    }
}
