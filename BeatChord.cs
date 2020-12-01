using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    /// <summary>
    ///   A chord thats used to determine an exact beat length
    /// </summary>
    public class BeatChord
    {
        public double Time { get; set; } = 0;

        public double Multiple { get; set; } = 0;

        public double MultipleRest
        {
            get
            {
                double rest = Multiple % 1;
                // Normalize
                if (rest > 0.5)
                {
                    rest = Math.Abs(rest - 1);
                }
                return rest;
            }
        }

        public BeatChord(double time, double firstTime, double beatLength)
        {
            Time = time;
            Multiple = (time - firstTime) / beatLength;
        }

        public override string ToString()
        {
            return $"{Multiple} at {Time}";
        }
    }
}
