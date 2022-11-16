using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    /// <summary>
    ///   Pair of Pitch & Octave
    /// </summary>
    public class ToneHeight
    {
        /// Static 
        //////////////

        public static ToneHeight C6 => new ToneHeight(PitchEnum.C, 6);
        public static ToneHeight D6 => new ToneHeight(PitchEnum.D, 6);
        public static ToneHeight E6 => new ToneHeight(PitchEnum.E, 6);
        public static ToneHeight F6 => new ToneHeight(PitchEnum.F, 6);
        public static ToneHeight G6 => new ToneHeight(PitchEnum.G, 6);
        public static ToneHeight A6 => new ToneHeight(PitchEnum.A, 6);
        public static ToneHeight B6 => new ToneHeight(PitchEnum.B, 6);

        private static readonly PitchEnum[] WhitePitches = new PitchEnum[]
        {
            PitchEnum.C,
            PitchEnum.D,
            PitchEnum.E,
            PitchEnum.F,
            PitchEnum.G,
            PitchEnum.A,
            PitchEnum.B
        };

        private static readonly PitchEnum[] BlackPitches = new PitchEnum[]
        {
            PitchEnum.Cis,
            PitchEnum.Es,
            PitchEnum.Fis,
            PitchEnum.Gis,
            PitchEnum.Bes,
        };

        /// Instance
        //////////////
        
        // Fields
        public PitchEnum Pitch;
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
                Pitch = (PitchEnum)(value % 12);
            }
        }

        public ToneHeight()
        {
        }

        public ToneHeight(PitchEnum pitch, int octave)
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

        private static WhitePitch GetWhitePitch(PitchEnum pitch)
        {
            // Parse first letter to WhitePitch
            if (Enum.TryParse(pitch.ToString().Substring(0, 1), out WhitePitch whitePitch))
            {
                return whitePitch;
            }
            else throw new Exception();

            //switch (pitch)
            //{
            //    case Pitch.C:
            //    case Pitch.Cis:
            //        return WhitePitch.C;
            //    case Pitch.D:
            //        return WhitePitch.D;
            //    case Pitch.Es:
            //    case Pitch.E:
            //        return WhitePitch.E;
            //    case Pitch.F:
            //    case Pitch.Fis:
            //        return WhitePitch.F;
            //    case Pitch.G:
            //    case Pitch.Gis:
            //        return WhitePitch.G;
            //    case Pitch.A:
            //        return WhitePitch.A;
            //    case Pitch.Bes:
            //    case Pitch.B:
            //        return WhitePitch.B;
            //    default:
            //        throw new Exception();
            //}
        }

        public static ToneHeight GetPreviousWhite(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case PitchEnum.A:
                case PitchEnum.B:
                case PitchEnum.D:
                case PitchEnum.E:
                case PitchEnum.G:
                    return toneHeight - 2;
                case PitchEnum.C:
                case PitchEnum.F:
                    return toneHeight - 1;
                default:
                    throw new Exception("No black key");
            }
        }

        public static ToneHeight GetPreviousBlack(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case PitchEnum.Bes:
                case PitchEnum.Es:
                case PitchEnum.Gis:
                    return toneHeight + 2;
                case PitchEnum.Cis:
                case PitchEnum.Fis:
                    return toneHeight - 3;
                default:
                    throw new Exception("No black key");
            }
        }

        public static ToneHeight GetNextWhite(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case PitchEnum.A:
                case PitchEnum.C:
                case PitchEnum.D:
                case PitchEnum.F:
                case PitchEnum.G:
                    return toneHeight + 2;
                case PitchEnum.B:
                case PitchEnum.E:
                    return toneHeight + 1;
                default:
                    throw new Exception("No black key");
            }
        }

        public static ToneHeight GetNextBlack(ToneHeight toneHeight)
        {
            switch (toneHeight.Pitch)
            {
                case PitchEnum.Cis:
                case PitchEnum.Fis:
                case PitchEnum.Gis:
                    return toneHeight + 2;
                case PitchEnum.Es:
                case PitchEnum.Bes:
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
            return first?.Pitch == second?.Pitch && first?.Octave == second?.Octave;
        }

        public static bool operator !=(ToneHeight first, ToneHeight second)
        {
            return first.Pitch != second.Pitch || first.Octave != second.Octave;
        }

        public override bool Equals(object obj)
        {
            if (obj is ToneHeight toneHeight)
            {
                return this == toneHeight;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Pitch.GetHashCode() ^ Octave.GetHashCode();
        }
    }

}
