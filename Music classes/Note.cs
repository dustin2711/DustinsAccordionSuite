using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public override string ToString()
        {
            string TiedNoteString(Note note, string name) => (note != null) ? (name + ": " + note.Id + "") : "";
            return $"{TiedNoteString(NoteBefore, "Before")}, {TiedNoteString(NoteAfter, "After")}";
        }
    }

    public class Note
    {
        // Static
        public static int IdCounter;

        // References
        public Beat Beat;
        public Voice Voice;

        public int Id;
        public ToneHeight ToneHeight;
        public double StartTime;
        public double EndTime;
        public Tiing Tiing;
        public List<ToneHeight> ChordToneHeights = new List<ToneHeight>();

        public double Portion
        {
            get => EndPortion - StartPortion;
            set
            {
                if (value < 0)
                {
                    Debugger.Break();
                }
                EndPortion = StartPortion + value;
            }
        }

        public double StartPortion
        {
            get => (StartTime - Beat.StartTime) / Beat.Duration;
            set => StartTime = Beat.StartTime + value * Beat.Duration;
        }

        public double EndPortion
        {
            get => (EndTime - Beat.StartTime) / Beat.Duration;
            set
            {
                if (value < 0 || value < StartPortion - Helper.µ)
                {
                    //Debugger.Break();
                }
                EndTime = Beat.StartTime + value * Beat.Duration;
            }
        }

        public double Duration => EndTime - StartTime;

        public Pitch Pitch => ToneHeight.Pitch;

        public int Octave => ToneHeight.Octave;

        // NoteLengths
        public NoteLength NoteLength => NoteLength.CreateFromPortion(Portion);

        public NoteTypeValue NoteTypeValue => NoteLength.NoteTypeValue;

        public Dotting Dotting => NoteLength.Dotting;

        public bool IsRest => ToneHeight == null;

        public Note(Beat beat)
        {
            Id = IdCounter++;
            Beat = beat;
        }

        public Note(Beat beat, Tone tone) : this(beat)
        {
            ToneHeight = tone.ToneHeight;
            StartTime = tone.StartTime;
            EndTime = tone.EndTime;
        }

        private Note(Beat beat, Voice voice, double startPortion, double endPortion) : this(beat)
        {
            Voice = voice;
            StartPortion = startPortion;
            EndPortion = endPortion;
        }

        public static Note CreateRest(Beat beat, Voice voice, double startPortion, double endPortion)
        {
            return new Note(beat, voice, startPortion, endPortion);
        }

        /// <summary>
        ///   Creates copy copiing Beat, Voice, ToneHeight and ChordToneHeights
        /// </summary>
        public Note(Note toCopy, bool copyTimeAndTiing = false)
        {
            Id = toCopy.Id + 10000;

            Beat = toCopy.Beat; 
            Voice = toCopy.Voice;
            ToneHeight = toCopy.ToneHeight;
            ChordToneHeights = toCopy.ChordToneHeights;

            if (copyTimeAndTiing)
            {
                StartTime = toCopy.StartTime;
                EndTime = toCopy.EndTime;
                Tiing = toCopy.Tiing;
            }
        }

        public string ToneHeightString
        {
            get
            {
                if (IsRest)
                {
                    return "Rest";
                }
                else
                {
                    string toneHeightString = ToneHeight.ToString();
                    if (ChordToneHeights.Count > 0)
                    {
                        toneHeightString += "-" + string.Join("-", ChordToneHeights);
                    }
                    return toneHeightString;
                }
            }
        }


        public override string ToString()
        {
            // Time respresentation
            //return $"{Beat.Number}: {ToneHeightString} {StartTime.ToString(3)} to {EndTime.ToString(3)}";

            // Portion respresentation
            return $"({Id}) {Beat.Number}-{Voice?.Id}: {ToneHeightString} {NoteLength} {(Tiing?.TiedType.ToString() ?? "")} from {StartPortion.ToString(4)} to {EndPortion.ToString(4)} ({Portion.ToString(4)})";
        }
    }

    /// <summary>
    ///   Presents a NoteLength by NoteTypeValue + Dotting.
    ///   This is a approximation of a time in seconds, thats why deviation exists giving the deviation from origin time
    /// </summary>
    public class NoteLength
    {
        // Debug
        public static bool AllowBreaking = false;

        public NoteTypeValue NoteTypeValue { set; get; }

        public Dotting Dotting { set; get; }

        public double ActualPortion { get; }

        /// <summary>
        ///   Always smaller than actual portion
        /// </summary>
        public double ProposedPortion { get; }

        public bool HasDeviation => Deviation > 0.001;

        public double Deviation => MakeItHarderDict[Dotting] * Helper.GetPercentageDistance(ActualPortion, ProposedPortion);

        private static Dictionary<Dotting, double> MakeItHarderDict = new Dictionary<Dotting, double>()
        {
            [Dotting.None] = 1,
            [Dotting.Dotted] = 2,
            [Dotting.DoubleDotted] = 8
        };

        public NoteLength(KeyValuePair<double, NoteTypeValue> valueForDuration, Dotting dotting, double actualPortion)
        {
            NoteTypeValue = valueForDuration.Value;
            Dotting = dotting;
            ActualPortion = actualPortion;
            ProposedPortion = FactorForDotting[dotting] * valueForDuration.Key;
        }

        public override string ToString()
        {
            string dottingString = "";
            if (Dotting == Dotting.Dotted)
            {
                dottingString += "•";
            }
            else if (Dotting == Dotting.DoubleDotted)
            {
                dottingString += "••";
            }

            string deviationString = HasDeviation ? $"🗲{ProposedPortion.ToString(3)} statt {ActualPortion.ToString(3)})" : "✔";

            return $"{NoteTypeValue}{dottingString}[{deviationString}]";
        }

        // Static section
        //////////////////

        public static NoteLength CreateFromPortion(double portion)
        {
            if (portion == 0)
            {
                return null;
            }

            List<NoteLength> noteLengths = new List<NoteLength>();
            /// If roundTolerance == 0.1 (10%), a note with 0.55 duration is only just converted to a note with 0.5 length (one half)
            foreach (KeyValuePair<double, NoteTypeValue> valueForDuration in NoteTypeValueForDuration)
            {
                void AddNoteLength(Dotting dotting)
                {
                    noteLengths.Add(new NoteLength(valueForDuration, dotting, portion));
                }

                AddNoteLength(Dotting.None);
                AddNoteLength(Dotting.Dotted);
                AddNoteLength(Dotting.DoubleDotted);
            }

            noteLengths = noteLengths.OrderBy(res => res.Deviation).ToList();
            // Get result with lowest deviation
            List<NoteLength> noteLengthsShort = noteLengths.Where(length => length.ProposedPortion < portion + Helper.µ).ToList();
            NoteLength winner = noteLengthsShort.FirstOrDefault() ?? noteLengths.FirstOrDefault();
            if (AllowBreaking && winner.Deviation > 0.001)
            {
                Debugger.Break();
            }
            return winner;
        }

        public static Dictionary<double, NoteTypeValue> NoteTypeValueForDuration { get; } = new Dictionary<double, NoteTypeValue>()
        {
            {4, NoteTypeValue.Long },
            {2, NoteTypeValue.Breve },
            {1, NoteTypeValue.Whole },
            {1.0 / 2, NoteTypeValue.Half },
            {1.0 / 4, NoteTypeValue.Quarter },
            {1.0 / 8, NoteTypeValue.Eighth },
            {1.0 / 16, NoteTypeValue.Item16Th },
            {1.0 / 32, NoteTypeValue.Item32Nd },
            {1.0 / 64, NoteTypeValue.Item64Th },
            {1.0 / 128, NoteTypeValue.Item128Th },
            {1.0 / 256, NoteTypeValue.Item256Th },
            {1.0 / 512, NoteTypeValue.Item512Th },
            {1.0 / 1024, NoteTypeValue.Item1024Th },
        };

        public static Dictionary<Dotting, double> FactorForDotting { get; } = new Dictionary<Dotting, double>()
        {
            [Dotting.None] = 1,
            [Dotting.Dotted] = 1.5,
            [Dotting.DoubleDotted] = 1.75,
        };
    }

    public enum Dotting
    {
        None = 0,
        Dotted = 128,
        DoubleDotted = 256,
    }
}
