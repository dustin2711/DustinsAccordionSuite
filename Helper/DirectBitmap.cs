using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public int[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new int[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public DirectBitmap(Bitmap original) : this(original.Width, original.Height)
        {
            Bitmap.CopyFrom(original);
        }

        public void SetPixel4(Point point, Color color) => SetPixel4(point.X, point.Y, color);

        public void SetPixel(Point point, Color color) => SetPixel(point.X, point.Y, color);

        public void SetPixel4(int x, int y, Color color)
        {
            SetPixel(x + 0, y + 0, color);
            SetPixel(x + 1, y + 0, color);
            SetPixel(x + 0, y + 1, color);
            SetPixel(x + 1, y + 1, color);
        }

        public void SetPixel(int x, int y, Color color)
        {
            int index = x + (y * Width);
            int col = color.ToArgb();

            Bits[index] = col;
        }

        public Color GetPixel(Point point) => GetPixel(point.X, point.Y);

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public bool IsBright(int x, int y, double brightness = 0.5)
        {
            return GetPixel(x, y).GetBrightness() > brightness;
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                Bitmap.Dispose();
                BitsHandle.Free();
            }
        }
    }
}
