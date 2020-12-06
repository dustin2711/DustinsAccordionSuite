﻿using System;
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

        public int TotalHeight
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
            height.TotalHeight = thisHeight.TotalHeight + value;
            return height;
        }

        public static ToneHeight operator -(ToneHeight thisHeight, int value)
        {
            ToneHeight height = new ToneHeight();
            height.TotalHeight = thisHeight.TotalHeight - value;
            return height;
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
