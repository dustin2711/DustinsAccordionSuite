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
    public enum Dotting { None, Single, Double, PlusQuarter }

    public enum Hand
    {
        Undefined,
        Left,
        Right
    }

    public class RestTone : Tone
    {
        protected RestTone() { }

        public RestTone(double startTime, double endTime, double beatDuration, BeatPart voice)
        {
            StartTime = startTime;
            EndTime = endTime;
            BeatDuration = beatDuration;
            BeatPart = voice;
        }

        public override string ToString()
        {
            return $"Rest {StartTime.ToString(3)} - {EndTime.ToString(3)} ({(BeatDuration != -1 ? (Value.ToString(3) + "th") : Duration.ToString(3))})";
         }
    }

    [Serializable]
    public class TiedTone : Tone
    {
        public TiedBeatNote TiedNote
        {
            get => BeatNote as TiedBeatNote;
            set => BeatNote = value as TiedBeatNote;
        }

        [XmlIgnore]
        public TiedType TiedType
        {
            get
            {
                if (ToneBefore != null && ToneAfter != null)
                {
                    return TiedType.Continue;
                }
                else if (ToneBefore != null)
                {
                    return TiedType.Stop;
                }
                else if (ToneAfter != null)
                {
                    return TiedType.Start;
                }
                else throw new Exception("No tied tone avilable");
            }
        }

        public TiedTone ToneBefore;
        public TiedTone ToneAfter;

        protected TiedTone() { }

        public TiedTone(Tone other) : base(other)
        {
        }

        protected override string SplitString => " [" + TiedType + "]";
    }

    [Serializable]
    [XmlInclude(typeof(TiedTone))]
    public class Tone
    {
        public BeatNote BeatNote;
        public NoteTypeValue NoteTypeValue;
        public Dotting Dotting;

        protected virtual string SplitString => "";

        public Hand Hand = Hand.Undefined;

        public ToneHeight ToneHeight;

        public double StartTime;

        /// <summary>
        ///   Color of the pressed key in the video (indicates left or right hand)
        [XmlIgnore]
        public Color Color;

        public List<Tone> ChordTones = new List<Tone>();

        public bool IsPartOfAnotherChord = false;
        public double EndTime;


        public double BeatDuration = -1;

        public BeatPart BeatPart = null;

        /// <summary>
        ///   e.g. 4th or 16th
        /// </summary>
        public double Value => BeatDuration / Duration; 

        public Pitch Pitch => ToneHeight.Pitch;

        /// <summary>
        ///   Duration / BeatDuration
        /// </summary>
        public double XmlDuration => Duration / BeatDuration;

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

        protected Tone(Tone other)
        {
            Hand = other.Hand;
            ToneHeight = other.ToneHeight;
            StartTime = other.StartTime;
            EndTime = other.EndTime;
            Color = other.Color;
            ChordTones = new List<Tone>(other.ChordTones);
            IsPartOfAnotherChord = other.IsPartOfAnotherChord;
            BeatDuration = other.BeatDuration;
        }

        /// <summary>
        ///   For serializing
        /// </summary>
        public Tone() { }

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

        public Tone[] SplitTone(double splitTime, double minimumLength)
        {
            double durationSplit1 = splitTime;
            double durationSplit2 = Duration - splitTime;
            bool split1Valid = durationSplit1 >= minimumLength;
            bool split2Valid = durationSplit2 >= minimumLength;

            Tone split1 = null;
            Tone split2 = null;
            if (split1Valid && split2Valid)
            {
                split1 = new TiedTone(this)
                {
                    Duration = durationSplit1,
                };
                split2 = new TiedTone(this)
                {
                    StartTime = StartTime + splitTime,
                    Duration = durationSplit2,
                };
                (split1 as TiedTone).ToneAfter = split2 as TiedTone;
                (split2 as TiedTone).ToneBefore = split1 as TiedTone;
            }
            else if (split1Valid)
            {
                split1 = new Tone(this)
                {
                    Duration = durationSplit1,
                };
            }
            else if (split2Valid)
            {
                split2 = new Tone(this)
                {
                    StartTime = StartTime + splitTime,
                    Duration = durationSplit2,
                };
            }

            return new Tone[2] { split1, split2 };
        }

        public override string ToString()
        {
            string pitches = Pitch.ToString();
            foreach (Tone tone in ChordTones)
            {
                pitches += "+" + tone.Pitch;
            }
            return $"{(BeatPart != null ? ("[" + BeatPart.VoiceId + "]") : "")}{SplitString} {pitches} {(IsPartOfAnotherChord ? " (part)" : "")},   " +
                $"{StartTime.ToString(3)} - {EndTime.ToString(3)} ({(BeatDuration != -1 ? (Value.ToString(3) + "th") : Duration.ToString(3))})" +
                $", Hue={Color.GetHue().ToShortString(0)}";
        }
    }
}