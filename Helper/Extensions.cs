﻿using CreateSheetsFromVideo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class Extensions
{
    public static string CutOut(this StringBuilder builder, int startIndex, int length)
    {
        if (length < 0)
        {
            throw new Exception("Length must be positive");
        }

        string textCutOut = builder.ToString(startIndex, length);
        builder.Remove(startIndex, length);
        return textCutOut;
    }

    //public static double Clamp(this double value, double min, double max)
    //{
    //    return Math.Max(Math.Min(value, max), min);
    //}

    public static T Clamp<T>(this T value, T min, T max) where T : IComparable
    {
        if (value.CompareTo(min) == -1) // value < min
        {
            return min;
        }
        else if (value.CompareTo(max) == 1) // value > max
        {
            return max;
        }
        else return value;
    }

    public static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {
        return !items.Any(predicate);
    }

    public static void RemoveRange<T>(this ICollection<T> items, IEnumerable<T> itemsToRemove)
    {
        foreach (T item in itemsToRemove)
        {
            items.Remove(item);
        }
    }

    public static bool First<T>(this IEnumerable<T> items, Func<T, bool> predicate, out T item)
    {
        item = items.FirstOrDefault(predicate);
        return item != null;
    }

    /// <summary>
    ///   Returns true if there are elements in list and all elements have same value.
    /// </summary>
    public static bool AllAreSame<T>(this IEnumerable<T> items)
    {
        if (items.Count() == 0)
        {
            return false;
            //throw new Exception("Not defined");
        }

        T firstItem = items.First();
        return items.All(item => item.Equals(firstItem));
    }

    public static int Alter(this PitchEnum pitch)
    {
        if (new PitchEnum[] { PitchEnum.Cis, PitchEnum.Fis, PitchEnum.Gis }.Contains(pitch))
        {
            return 1;
        }
        else if (new PitchEnum[] { PitchEnum.Es, PitchEnum.Bes }.Contains(pitch))
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    public static string ToString(this double value, int decimals = 2)
    {
        return value.ToString("0." + new string('0', decimals));
    }

    public static string ToShortString(this float value, int decimals = 2)
    {
        return value.ToString("0." + new string('0', decimals));
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

    /// <summary>
    ///   Removes and returns item at the given index.
    /// </summary>
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

    public static double Mean(this IEnumerable<double> someDoubles)
    {
        return someDoubles.Sum() / someDoubles.Count();
    }

    public static string ToLog<T>(this IEnumerable<T> values, int count = 10)
    {
        return string.Join(",", values.Take(count));
    }

    /// <summary>
    ///   Double = relative delta, Float = absolute delta
    /// </summary>
    public static bool IsAboutRelative(this double value, double otherValue, double relativeDelta = 0.1)
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
    public static bool IsAboutAbsolute(this double value, double otherValue, double absoluteDelta = Helper.µ)
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

    public static bool IsIn<T>(this T obj, params T[] items)
    {
        return items.Contains(obj);
    }

    public static bool FirstOrDefault<T>(this IEnumerable<T> list, Func<T, bool> func, out T element)
    {
        element = list.FirstOrDefault(func);
        return element != null;
    }

    /// <summary>
    ///   Returns next value in enum. When over lenth, repeats from beginning.
    /// </summary>
    public static T Next<T>(this T value) where T : struct
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException(string.Format("Argument {0} is not an Enum", typeof(T).FullName));
        }

        T[] array = (T[])Enum.GetValues(value.GetType());
        int index = Array.IndexOf(array, value) + 1;
        return (index == array.Length)
            ? array[0] :
            array[index];
    }

    /// <summary>
    ///   Returns previous value in enum. When under 0, repeats from end.
    /// </summary>
    public static T Previous<T>(this T value) where T : struct
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException(string.Format("Argument {0} is not an Enum", typeof(T).FullName));
        }

        T[] array = (T[])Enum.GetValues(value.GetType());
        int index = Array.IndexOf(array, value) - 1;
        return (index == -1)
            ? array[array.Length - 1]
            : array[index];
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
