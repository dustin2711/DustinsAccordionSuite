using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{

    public struct ToneHeight
    {
        /// Static 
        //////////////
        ///
        private readonly static Pitch[] WhitePitches = new Pitch[]
        {
            Pitch.C,
            Pitch.D,
            Pitch.E,
            Pitch.F,
            Pitch.G,
            Pitch.A,
            Pitch.B
        };

        private readonly static Pitch[] BlackPitches = new Pitch[]
        {
            Pitch.Cis,
            Pitch.Es,
            Pitch.Fis,
            Pitch.Gis,
            Pitch.Bes,
        };

        /// Instance
        //////////////
        
        // Fields
        public Pitch Pitch;
        public int Octave;

        /// <summary>
        ///   Calcs a total value = octave * 12 + pitch
        /// </summary>
        public int TotalValue
        {
            get => Octave * 12 + (int)Pitch;
            set
            {
                Octave = value / 12;
                Pitch= (Pitch)(value % 12);
            }
        }

        public ToneHeight(Pitch pitch, int octave)
        {
            Pitch = pitch;
            Octave = octave;
        }

        public static ToneHeight operator +(ToneHeight thisHeight, int value)
        {
            ToneHeight height = new ToneHeight();
            height.TotalValue = thisHeight.TotalValue + value;
            return height;
        }

        public static ToneHeight operator -(ToneHeight thisHeight, int value)
        {
            ToneHeight height = new ToneHeight();
            height.TotalValue = thisHeight.TotalValue - value;
            return height;
        }

        /// <summary>
        ///   Gets the number of the key on the keyboard or in sheets (black keys belong to white keys)
        /// </summary>
        public int GetKeyNumber()
        {
            int pitchStake = (int)GetWhitePitch(Pitch);
            return 7 * Octave + pitchStake;
        }

        private static WhitePitch GetWhitePitch(Pitch pitch)
        {
            switch (pitch)
            {
                case Pitch.C:
                case Pitch.Cis:
                    return WhitePitch.C;
                case Pitch.D:
                    return WhitePitch.D;
                case Pitch.Es:
                case Pitch.E:
                    return WhitePitch.E;
                case Pitch.F:
                case Pitch.Fis:
                    return WhitePitch.F;
                case Pitch.G:
                case Pitch.Gis:
                    return WhitePitch.G;
                case Pitch.A:
                    return WhitePitch.A;
                case Pitch.Bes:
                case Pitch.B:
                    return WhitePitch.B;
                default:
                    throw new Exception();
            }
        }

        public static ToneHeight GetPreviousWhite(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case Pitch.A:
                case Pitch.B:
                case Pitch.D:
                case Pitch.E:
                case Pitch.G:
                    return toneHeight - 2;
                case Pitch.C:
                case Pitch.F:
                    return toneHeight - 1;
                default:
                    throw new Exception("No black key");
            }
        }

        public static ToneHeight GetPreviousBlack(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case Pitch.Bes:
                case Pitch.Es:
                case Pitch.Gis:
                    return toneHeight + 2;
                case Pitch.Cis:
                case Pitch.Fis:
                    return toneHeight - 3;
                default:
                    throw new Exception("No black key");
            }
        }

        public static ToneHeight GetNextWhite(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case Pitch.A:
                case Pitch.C:
                case Pitch.D:
                case Pitch.F:
                case Pitch.G:
                    return toneHeight + 2;
                case Pitch.B:
                case Pitch.E:
                    return toneHeight + 1;
                default:
                    throw new Exception("No black key");
            }
        }

        public static ToneHeight GetNextBlack(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case Pitch.Cis:
                case Pitch.Fis:
                case Pitch.Gis:
                    return toneHeight + 2;
                case Pitch.Es:
                case Pitch.Bes:
                    return toneHeight + 3;
                default:
                    throw new Exception("No black key");
            }
        }

        public override string ToString()
        {
            return $"{Pitch}{Octave}";
        }

        public static bool operator ==(ToneHeight first, ToneHeight second)
        {
            return first.Pitch == second.Pitch && first.Octave == second.Octave;
        }

        public static bool operator !=(ToneHeight first, ToneHeight second)
        {
            return first.Pitch != second.Pitch || first.Octave != second.Octave;
        }
    }

}
