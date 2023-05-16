using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    enum AcchordType
    {
        Undefined,
        Dur,
        Mol,
        Sept,
        Min
    }

    enum HandType
    {
        Left,
        Both,
        Right
    }

    enum AppMode
    {
        Save, // Play and SAVE
        Load // LOAD and evaluate
    }

    /// <summary>
    ///   Color of which tones shall be noted?
    /// </summary>
    enum ColorMode
    {
        /// <summary>
        ///   Collect only hue < 180
        /// </summary>
        Green,
        /// <summary>
        ///   Collect only hue > 180
        /// </summary>
        Blue,
        /// <summary>
        ///   Color does not matter for collecting tones
        /// </summary>
        All
    }

    /// <summary>
    ///   
    /// </summary>
    public enum PitchEnum
    {
        C,
        Cis,
        D,
        Es,
        E,
        F,
        Fis,
        G,
        Gis,
        A,
        Bes,
        B,
    }

    public enum WhitePitch
    {
        C,
        D,
        E,
        F,
        G,
        A,
        B,
    }

    public static class PitchExtensions
    {
        public static int GetAlter(this PitchEnum pitch)
        {
            string pitchString = pitch.ToString();
            if (pitchString.Contains("is"))
            {
                return 1;
            }
            else if (pitchString.Contains("es"))
            {
                return -1;
            }
            else return 0;
        }

        public static Step GetStep(this PitchEnum pitch)
        {
            if (Enum.TryParse(pitch.ToString().Substring(0, 1), out Step step))
            {
                return step;
            }
            else
            {
                throw new Exception("Could not get Step-enum from pitch");
            }
        }

        public static void ApplyAlter(this PitchEnum pitch, int alter)
        {

        }
    }
}
