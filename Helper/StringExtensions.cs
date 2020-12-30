using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    public static class StringExtensions
    {
        public static double FilterDouble(this string text)
        {
            bool IsPartOfNumber(char c) => char.IsDigit(c) || c == '.' || c == ',';

            return Convert.ToDouble(string.Join("", text.Where(IsPartOfNumber)));
        }

        public static int FilterInteger(this string text)
        {
            return Convert.ToInt32(string.Join("", text.Where(char.IsDigit)));
        }

        public static string RemoveDigits(this string text)
        {
            return new string(text.Where(c => c < '0' || c > '9').ToArray());
        }

        public static IEnumerable<int> AllIndexesOf(this string str, string searchstring)
        {
            int minIndex = str.IndexOf(searchstring);
            while (minIndex != -1)
            {
                yield return minIndex;
                minIndex = str.IndexOf(searchstring, minIndex + searchstring.Length);
            }
        }

        public static int FindIndexBefore(this string text, int startIndex, string searchString, bool addSearchstringLength = false)
        {
            for (int index = startIndex; index >= 0; index--)
            {
                if (text.Substring(index, searchString.Length) == searchString)
                {
                    return index + (addSearchstringLength ? searchString.Length : 0);
                }
            }

            return -1;
        }

        public static int FindIndexAfter(this string text, int startIndex, string searchString, bool addSearchstringLength = false)
        {
            for (int index = startIndex; index < text.Length; index++)
            {
                if (text.Substring(index, searchString.Length) == searchString)
                {
                    return index + (addSearchstringLength ? searchString.Length : 0);
                }
            }

            return -1;
        }

    }
}
