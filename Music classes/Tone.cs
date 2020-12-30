using Accord.Video.FFMPEG;
using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;

namespace CreateSheetsFromVideo
{
    public enum Dotting { None, Single, Double, PlusQuarter, IsZero }

    public enum Hand { Undefined, Left, Right }

    [Serializable]
    public class Tone
    {
        public NoteTypeValue NoteTypeValue;
        public Dotting Dotting;

        public Hand Hand = Hand.Undefined;

        public ToneHeight ToneHeight;

        public double StartTime;
        public double EndTime;

        /// <summary>
        ///   Color of the pressed key in the video (indicates left or right hand)
        [XmlIgnore]
        public Color Color;

        public Pitch Pitch => ToneHeight.Pitch;

        public int Octave => ToneHeight.Octave;

        [XmlElement("Color")]
        public string ColorHtml
        {
            get => ColorTranslator.ToHtml(Color);
            set => Color = ColorTranslator.FromHtml(value);
        }

        /// <summary>
        ///   Duration in seconds
        /// </summary>
        public double Duration
        {
            get => EndTime - StartTime;
            set => EndTime = StartTime + value;
        }

        /// <summary>
        ///   For serializing
        /// </summary>
        protected Tone() { }

        public Tone(ToneHeight toneHeight, Color color, double startTime)
        {
            ToneHeight = toneHeight;
            Color = color;
            StartTime = startTime;
        }

        public Tone(ToneHeight toneHeight, double startTime, double duration)
        {
            ToneHeight = toneHeight;
            StartTime = startTime;
            Duration = duration;
        }

        public override string ToString()
        {
            return $"{ToneHeight}: {StartTime.ToString(3)} - {EndTime.ToString(3)}" +
                $", Hue={Color.GetHue().ToShortString(0)}";
        }
    }
}