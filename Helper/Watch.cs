using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CreateSheetsFromVideo
{
    public static class Watch
    {
        public static Stopwatch watch = new Stopwatch();

        public static void Start()
        {
            watch.Restart();
        }

        public static void Measure(Action<string> logAction, string infoText = "")
        {
            //watch.Stop();
            if (infoText != "")
            {
                infoText += ": ";
            }
            logAction(infoText + watch.Elapsed.TotalMilliseconds + " ms");
        }
    }
}
