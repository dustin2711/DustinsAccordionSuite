using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    public class PianoKey
    {
        public ToneHeight ToneHeight { get; set; }
        public Point Point { get; }
        public double DistanceToNext { get; set; }
        public Color NotPressedColor { get; }

        public PianoKey(Point point, DirectBitmap bitmap)
        {
            Point = point;
            NotPressedColor = bitmap.GetPixel(Point);
        }

        public int Octave => ToneHeight.Octave;
        public PitchEnum Pitch => ToneHeight.Pitch;

        public override string ToString()
        {
            return $"({Point.X} | {Point.Y}): {ToneHeight}, {NotPressedColor}";
        }
    }
}
