using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    /// <summary>
    ///   A pitch and its integer value. Usable for sorting by pitch.
    /// </summary>
    public class PitchIntegerPair
    {
        public PitchEnum Pitch { get; }
        public int Integer { get; }

        public PitchIntegerPair(PitchEnum pitch)
        {
            Pitch = pitch;
            Integer = (int)pitch;
        }

        public override string ToString()
        {
            return Pitch.ToString() + " / " + Integer;
        }
    }
}
