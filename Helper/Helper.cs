using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;

namespace CreateSheetsFromVideo
{
    public struct Span<T>
    {
        public T Start;
        public T End;

        public Span(T start, T end)
        {
            Start = start;
            End = end;
        }

    }

    public class SingleInstance<T> where T : SingleInstance<T>
    {
        public T Instance;

        protected SingleInstance()
        {
            Instance = this as T;
        }
    }

    public static class Helper
    {
        /// <summary>
        ///   0.000001
        /// </summary>
        public static double µ => 0.000001;

        /// <summary>
        ///   0.001
        /// </summary>
        public static double m => 0.001;

        /// <summary>
        ///   Returns how much the bigger number is bigger than the small number (always positive values).
        ///   E.g. Inputs 2.2 and 2.0 => Returns 0.1 (2.2 is 10 % bigger than 2.0)
        /// </summary>
        public static double GetPercentageDistance(double a, double b)
        {
            if (a == 0 || b == 0)
            {
                // Cannot divide by 0
                return -1;
            }
            else if (a >= b)
            {
                return (a / b) - 1;
            }
            else
            {
                return (b / a) - 1;
            }
        }

        public static string TimeStringFromMs(double milliseconds)
        {
            return TimeSpan.FromMilliseconds(milliseconds).ToString(@"mm\:ss\:fff");
        }

        private static Color CreateColorless(byte brightness)
        {
            return Color.FromArgb(brightness, brightness, brightness);
        }

        public static void OpenWithDefaultProgram(string path, string arguments = "")
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = $"\"{path}\" {arguments}";
            fileopener.Start();
        }
    }
}
