using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateSheetsFromVideo
{
    public class Tiing
    {
        public Note NoteBefore;
        public Note NoteAfter;

        public Tiing(Note noteBefore = null, Note noteAfter = null)
        {
            NoteBefore = noteBefore;
            NoteAfter = noteAfter;
        }

        [XmlIgnore]
        public TiedType TiedType
        {
            get
            {
                if (NoteBefore != null && NoteAfter != null)
                {
                    return TiedType.Continue;
                }
                else if (NoteBefore != null)
                {
                    return TiedType.Stop;
                }
                else if (NoteAfter != null)
                {
                    return TiedType.Start;
                }
                else throw new Exception("No tied tone avilable");
            }
        }
    }

    public class Note
    {
        // References
        public Beat Beat;
        public Voice Voice;

        public ToneHeight ToneHeight;
        public double StartTime;
        public double EndTime;
        public Tiing Tiing;
        public List<ToneHeight> ChordToneHeights = new List<ToneHeight>();

        public double Portion
        {
            get => EndPortion - StartPortion;
            set => EndPortion = StartPortion + value;
        }

        public double StartPortion
        {
            get => (StartTime - Beat.StartTime) / Beat.Duration;
            set => StartTime = Beat.StartTime + value * Beat.Duration;
        }

        public double EndPortion
        {
            get => (EndTime - Beat.StartTime) / Beat.Duration;
            set => EndTime = Beat.StartTime + value * Beat.Duration;
        }

        public double Duration => EndTime - StartTime;

        public Pitch Pitch => ToneHeight.Pitch;

        public int Octave => ToneHeight.Octave;

        // NoteLengths
        public NoteLength NoteLength => NoteLength.CreateFromPortion(Portion);

        public NoteTypeValue NoteTypeValue => NoteLength.NoteTypeValue;

        public Dotting Dotting => NoteLength.Dotting;

        public Note(Beat beat)
        {
            Beat = beat;
        }

        public Note(Beat beat, Tone tone) : this(beat)
        {
            ToneHeight = tone.ToneHeight;
            StartTime = tone.StartTime;
            EndTime = tone.EndTime;
        }

        /// <summary>
        ///   Creates copy
        /// </summary>
        public Note(Note toCopy)
        {
            Beat = toCopy.Beat;
            Voice = toCopy.Voice;
            StartTime = toCopy.StartTime;
            EndTime = toCopy.EndTime;
            ToneHeight = toCopy.ToneHeight;
            Tiing = toCopy.Tiing;
            ChordToneHeights = toCopy.ChordToneHeights;
        }

        public virtual string ToneHeightString
        {
            get
            {
                string toneHeightString = ToneHeight.ToString();
                if (ChordToneHeights.Count > 0)
                {
                    toneHeightString += "-" + string.Join("-", ChordToneHeights);
                }
                return toneHeightString;
            }
        }


        public override string ToString()
        {
            // Time respresentation
            //return $"{Beat.Number}: {ToneHeightString} {StartTime.ToString(3)} to {EndTime.ToString(3)}";

            // Portion respresentation
            return $"{Beat.Number}: {ToneHeightString} {NoteLength} from {StartPortion.ToString(4)} to {EndPortion.ToString(4)} ({Portion.ToString(4)})";
        }
    }

    public class Rest : Note
    {
        public Rest(Beat beat, Voice voice, double startPortion, double endPortion) : base(beat)
        {
            Voice = voice;
            StartPortion = startPortion;
            EndPortion = endPortion;
        }

        //public Rest(Note toCopy) : base(toCopy)
        //{
        //}

        public override string ToneHeightString => "Rest";
    }
}
