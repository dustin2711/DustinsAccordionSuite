using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    enum AcchordType
    {
        Dur,
        Mol,
        Sept,
        SeptMin
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

    public enum Pitch
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
}
