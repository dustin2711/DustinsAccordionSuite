﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;

namespace CreateSheetsFromVideo
{
    public static class Helper
    {
        /// <summary>
        ///   E.g. Inputs 2.2 and 2.0 result in 0.1 (10%)
        /// </summary>
        public static double GetPercentageDistance(double a, double b)
        {
            if (a == b)
            {
                return 0;
            }
            else if (a == 0 || b == 0)
            {
                // Cannot divide by 0
                return -1;
            }
            else if (a > b)
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
    }
}