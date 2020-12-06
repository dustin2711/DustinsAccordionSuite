using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreateSheetsFromVideo
{

    public static class Extensions
    {
        public static string RemoveDigits(this string text)
        {
            return new string(text.Where(c => c < '0' || c > '9').ToArray());
        }

        public static string ToShortString(this double value, string format = "0.000")
        {
            return value.ToString(format);
        }

        public static string ToShortString(this float value, string format = "0.000")
        {
            return value.ToString(format);
        }

        public static void Invoke(this Control control, MethodInvoker action)
        {
            int i = 0;
            while (!control.Visible)
            {
                if (i++ == 40)
                {
                    throw new Exception("Waited too long");
                }
                System.Threading.Thread.Sleep(50);
            }

            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public static T Pop<T>(this List<T> list, int index)
        {
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public static double StandardDeviation(this IEnumerable<int> someInts)
        {
            if (someInts.Count() == 0)
            {
                return 0;
            }

            double average = someInts.Average();
            double sumOfSquaresOfDifferences = someInts.Select(val => (val - average) * (val - average)).Sum();
            double deviation = Math.Sqrt(sumOfSquaresOfDifferences / someInts.Count());
            return deviation;
        }

        public static double StandardDeviation(this IEnumerable<double> someDoubles)
        {
            if (someDoubles.Count() == 0)
            {
                return 0;
            }

            double average = someDoubles.Average();
            double sumOfSquaresOfDifferences = someDoubles.Select(val => (val - average) * (val - average)).Sum();
            double deviation = Math.Sqrt(sumOfSquaresOfDifferences / someDoubles.Count());
            return deviation;
        }

        public static string ToLog<T>(this IEnumerable<T> values, int count = 20)
        {
            return string.Join(",", values.Take(10));
        }

        /// <summary>
        ///   Double = relative delta, Float = absolute delta
        /// </summary>
        public static bool IsAboutRel(this double value, double otherValue, double relativeDelta = 0.1)
        {
            if (value == 0 && otherValue == 0)
            {
                return true;
            }
            else if (value == 0 || otherValue == 0)
            {
                return false;
            }
            else
            {
                double division = value / otherValue;
                return (1 - relativeDelta) < division && division < (1 + relativeDelta);
            }
        }

        /// <summary>
        ///   Math.Abs(value - otherValue) <= absoluteDelta
        /// </summary>
        public static bool IsAboutAbs(this double value, double otherValue, double absoluteDelta)
        {
            return Math.Abs(value - otherValue) <= absoluteDelta;
        }

        public static bool SimilarColorsAs(this Color color, Color otherColor, int allowedDelta = 50)
        {
            return
                 (Math.Abs(color.R - otherColor.R)
                + Math.Abs(color.G - otherColor.G)
                + Math.Abs(color.B - otherColor.B)) < allowedDelta;
        }

        public static int DeltaTo(this Color color, Color otherColor)
        {
            return
                  Math.Abs(color.R - otherColor.R)
                + Math.Abs(color.G - otherColor.G)
                + Math.Abs(color.B - otherColor.B);
        }

        /// <summary>
        ///   Equals "Enumerable.Contains(this)".
        /// </summary>
        public static bool IsIn<T>(this T obj, IEnumerable<T> list)
        {
            return list.Contains(obj);
        }

        public static bool TryGet<T>(this IEnumerable<T> list, Func<T, bool> func, out T element)
        {
            element = list.FirstOrDefault(func);
            return element != null;
        }

        public static T Next<T>(this T source) where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException(string.Format("Argument {0} is not an Enum", typeof(T).FullName));
            }

            T[] array = (T[])Enum.GetValues(source.GetType());
            int j = Array.IndexOf(array, source) + 1;
            return (j == array.Length) ? array[0] : array[j];
        }

        public static T Previous<T>(this T source) where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException(string.Format("Argument {0} is not an Enum", typeof(T).FullName));
            }

            T[] array = (T[])Enum.GetValues(source.GetType());
            int j = Array.IndexOf(array, source) - 1;
            return (j == -1) ? array[array.Length - 1] : array[j];
        }

        public static void CopyFrom(this Bitmap bitmap, Bitmap toCopy)
        {
            Rectangle rect = new Rectangle(0, 0, toCopy.Width, toCopy.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(toCopy, rect, rect, GraphicsUnit.Pixel);
                graphics.Dispose();
            }
        }

        public static Bitmap Copy(this Bitmap bitmap)
        {
            Bitmap copy = new Bitmap(bitmap.Width, bitmap.Height);
            copy.CopyFrom(bitmap);
            return copy;
        }

        public static void SetPixel4(this Bitmap bitmap, int x, int y, Color color)
        {
            x = Math.Min(x, bitmap.Width - 2);
            y = Math.Min(y, bitmap.Height - 2);
            bitmap.SetPixel(x, y, color);
            bitmap.SetPixel(x + 1, y, color);
            bitmap.SetPixel(x, y + 1, color);
            bitmap.SetPixel(x + 1, y + 1, color);
        }

        public static void SetPixel16(this Bitmap bitmap, int x, int y, Color color)
        {
            x = Math.Min(x, bitmap.Width - 4);
            y = Math.Min(y, bitmap.Height - 4);
            bitmap.SetPixel4(x - 1, y - 1, color);
            bitmap.SetPixel4(x + 1, y - 1, color);
            bitmap.SetPixel4(x - 1, y + 1, color);
            bitmap.SetPixel4(x + 1, y + 1, color);
        }
    }
}
