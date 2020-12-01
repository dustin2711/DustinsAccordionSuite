using Accord.Video.FFMPEG;
using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;

namespace CreateSheetsFromVideo
{
    public enum Hand
    {
        Undefined,
        Left,
        Right
    }

    [Serializable]
    public class Tone
    {
        public Hand Hand = Hand.Undefined;

        public Pitch Pitch { get; set; }

        public double StartTime { get; set; }

        /// <summary>
        ///   Color of the pressed key in the video (indicates left or right hand)
        [XmlIgnore]
        public Color Color { get; set; }

        public StartStop? StartStop { get; set; } = null;

        public List<Tone> ChordTones { get; set; } = new List<Tone>();

        public bool IsPartOfAnotherChord { get; set; } = false;
        public double EndTime { get; set; }

        //public double TimeToBeatEnd => BeatEndTime - StartTime;
        //public bool IsLastInBeat { get; set; }
        //public double BeatEndTime { get; set; } = -1;



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
        public Tone() { }

        private Tone(Tone other)
        {
            Hand = other.Hand;
            Pitch = other.Pitch;
            StartTime = other.StartTime;
            EndTime = other.EndTime;
            Color = other.Color;
            ChordTones = new List<Tone>(other.ChordTones);
            IsPartOfAnotherChord = other.IsPartOfAnotherChord;
        }

        /// <summary>
        ///  For debugging
        /// </summary>
        public Tone(Pitch pitch, Color color)
        {
            Pitch = pitch;
            Color = color;
        }

        public Tone(Pitch pitch, Color color, double startTime)
        {
            Pitch = pitch;
            Color = color;
            StartTime = startTime;
        }

        public Tone[] SplitTone(double splitTime)
        {
            Tone split1 = new Tone(this)
            {
                Duration = splitTime,
                StartStop = MusicXmlSchema.StartStop.Start
            };
            Tone split2 = new Tone(this)
            {
                StartTime = StartTime + splitTime,
                Duration = Duration - splitTime,
                StartStop = MusicXmlSchema.StartStop.Stop
            };

            return new Tone[2] { split1, split2 };
        }

        public override string ToString()
        {
            string pitches = Pitch.ToString();
            foreach (Tone tone in ChordTones)
            {
                pitches += "+" + tone.Pitch;
            }
            return $"{pitches} {(IsPartOfAnotherChord ? " (part)" : "")},   " +
                $"{StartTime.ToShortString()} - {EndTime.ToShortString()} ({Duration.ToShortString()})" +
                $", Hue={Color.GetHue().ToShortString()}";
        }
    }

}
