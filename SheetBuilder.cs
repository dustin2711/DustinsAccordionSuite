using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using XmlNote = MusicXmlSchema.Note;

// NoteDuration = Time in seconds e.g. 0.122
// NoteLength = Time in Beats e.g. 1/16

/// <summary>
///   ToDo:
///     - Triolen einführen 8)
///     - Notenhälse oben umkehren (+ mehr Abstand zw. Zeilen?
///     - Ganz zum Schluss: Basstakt hat 6 Schläge
///     - Ganz zum Schluss: Bass-Beschriftung ist zu eng
/// </summary>
namespace CreateSheetsFromVideo
{
    public class SheetsBuilder
    {
        private const string BackupFootnote = "Backup";

        // Technical settings
        public const double BeatOffsetPortion = 0; // Positive = shifts Beats right
        private const int OctaveOffset = -2;
        private const int FourDivisions = 1; // Divisions per Quarter (MuseScore does not care about this value)

        private ScorePartwise scorePartwise;
        private ScorePartwisePart leftPart;
        private ScorePartwisePart rightPart;

        /// <summary>
        ///   The counter of the current beat (2 because first beat is automatically created)
        /// </summary>
        private int beatNumber = 2;

        public SheetsBuilder(SheetSave save, string title)
        {
            // Check for tones
            Debug.Assert(save.Tones.Count > 0);

            // Order tones by starttime
            List<Tone> tones = save.Tones.OrderBy(t => t.StartTime).ToList();

            // Create score
            scorePartwise = CreateScorePartwise(title, save.BeatValues.Duration, out leftPart, out rightPart);

            Score score = new Score(save);
            foreach (Beat beat in score.Beats)
            {
                if (beat != score.Beats.First())
                {
                    AddBeat();
                }

                foreach (Voice voice in beat.Voices)
                {
                    foreach (Note tone in voice.Notes)
                    {
                        AddNoteRight(tone);
                    }

                    // Insert backup
                    if (voice.Notes.Count > 0)
                    {
                        AddBackupRight();
                    }
                }
            }
        }

        void AddBeat()
        {
            leftPart.Measure.Add(new ScorePartwisePartMeasure()
            {
                Number = beatNumber.ToString(),
                Width = 192
            });
            rightPart.Measure.Add(new ScorePartwisePartMeasure()
            {
                Number = beatNumber.ToString(),
                Width = 192
            });
            beatNumber++;
        }

        void AddNoteRight(Note Note)
        {
            foreach (XmlNote note in XmlNoteFromNote(Note))
            {
                rightPart.Measure[beatNumber - 2].Note.Add(note);
            }
        }

        void AddBackupLeft()
        {
            AddBackup(leftPart);
        }

        void AddBackupRight()
        {
            AddBackup(rightPart);
        }

        // This is always a whole beat
        void AddBackup(ScorePartwisePart part)
        {
            rightPart.Measure[beatNumber - 2].Note.Add(new XmlNote()
            {
                Duration = FourDivisions,
                Footnote = new FormattedText() { Value = BackupFootnote }
            });
        }

        private XmlNote[] XmlNoteFromNote(Note note)
        {
            // Create note
            XmlNote xmlNote;
            if (note is Rest)
            {
                xmlNote = new XmlNote()
                {
                    Rest = new MusicXmlSchema.Rest()
                };
            }
            else
            {
                int alter = note.Pitch.GetAlter();
                xmlNote = new XmlNote()
                {
                    Chord = (note.ChordToneHeights.Count > 0) ? new Empty() : null,
                    Pitch = new MusicXmlSchema.Pitch()
                    {
                        Step = note.Pitch.GetStep(),
                        Alter = alter,
                        AlterSpecified = alter != 0,
                        Octave = (OctaveOffset + note.Octave).ToString() // 4 is default
                    },
                    Stem = new Stem() { Value = note.Pitch > Pitch.C ? StemValue.Down : StemValue.Up },
                };
            }

            // Common values
            xmlNote.Type = new NoteType()
            {
                Value = note.NoteTypeValue
            };
            xmlNote.Duration = FourDivisions * (decimal)note.Portion;
            xmlNote.Voice = (note.Voice.Id + 1).ToString();

            string xmlNoteAsString = $"{(xmlNote.Rest != null ? "Rest" : $"{xmlNote.Pitch?.Step}{xmlNote.Pitch?.Alter}")}: {xmlNote.Duration}/{xmlNote.Type.Value}";

            if (xmlNote.Duration > 1)
                Debugger.Break();

            // Handle tiing
            if (note.Tiing != null)
            {
                // btw "note.Tie.Add()" does not work
                Notations notations = new Notations()
                {
                    Tied =
                    {
                        new Tied()
                        {
                            Type = note.Tiing.TiedType
                        }
                    }
                };
                xmlNote.Notations.Add(notations);// { Tied = new System.Collections.ObjectModel.Collection<Tied>() { new Tied() { Type = TiedType.Stop } } });
            }

            if (note.Dotting.HasFlag(Dotting.None))
            {
                // Do nothing
            }
            if (note.Dotting.HasFlag(Dotting.Dotted))
            {
                xmlNote.Dot.Add(new EmptyPlacement());
            }
            else if (note.Dotting.HasFlag(Dotting.DoubleDotted))
            {
                xmlNote.Dot.Add(new EmptyPlacement());
                xmlNote.Dot.Add(new EmptyPlacement());
            }
            else 
            //if (note.Dotting.HasFlag(Dotting.PlusQuarter)
            //    || note.Dotting.HasFlag(Dotting.PlusEighth)
            //    || note.Dotting.HasFlag(Dotting.Plus3Eighth)
            //    || note.Dotting.HasFlag(Dotting.Plus16th))
            {
                decimal bigPortion = 1m / (decimal)NoteLength.FactorForDotting[note.Dotting]; // e.g. f(1¼) = 1 / 1.25 = 0.8 
                // Adapt main note duration
                xmlNote.Duration = bigPortion * (decimal)note.Duration;
                // Set extra note duration (1/4x or 1/8x or 1/16x)
                XmlNote extraXmlNote = CopyXmlNote(xmlNote);
                extraXmlNote.Duration = (1m - bigPortion) * (decimal)note.Duration;
                //if (note.Dotting.HasFlag(Dotting.Plus16th))
                //{
                //    Debug.Assert((int)note.NoteTypeValue >= (int)NoteTypeValue.Item64Th); // Else cannot take 1/16
                //    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous().Previous().Previous(); // Add 1/16
                //}
                //else if (note.Dotting.HasFlag(Dotting.PlusEighth))
                //{
                //    Debug.Assert((int)note.NoteTypeValue >= (int)NoteTypeValue.Item128Th); // Else cannot take 1/8
                //    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous().Previous(); // Add 1/8
                //}
                //else if (note.Dotting.HasFlag(Dotting.Plus3x16th))
                //{
                //    Debug.Assert((int)note.NoteTypeValue >= (int)NoteTypeValue.Item128Th); // Else cannot take 1/8
                //    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous().Previous(); // Add 1/8
                //    extraXmlNote.Dot.Add(new EmptyPlacement()); // Add half of 1/8 => 3/16
                //}
                //else if (note.Dotting.HasFlag(Dotting.PlusQuarter))
                //{
                //    Debug.Assert((int)note.NoteTypeValue >= (int)NoteTypeValue.Item256Th); // Else cannot take 1/4
                //    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous(); // Add 1/4
                //}
                //else if (note.Dotting.HasFlag(Dotting.Plus3Eighth))
                //{
                //    Debug.Assert((int)note.NoteTypeValue >= (int)NoteTypeValue.Item128Th); // Else cannot take 1/4
                //    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous(); // Add 1/4
                //    extraXmlNote.Dot.Add(new EmptyPlacement()); // Add half of 1/4 => 3/8
                //}

                if (note.Tiing?.TiedType is TiedType type)
                {
                    if (type == TiedType.Start)
                    {
                        extraXmlNote.Notations.First().Tied.First().Type = TiedType.Continue;
                    }
                    else if (type == TiedType.Stop)
                    {
                        xmlNote.Notations.First().Tied.First().Type = TiedType.Continue;
                        extraXmlNote.Notations.First().Tied.First().Type = TiedType.Stop;
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }

                return new XmlNote[] { xmlNote, extraXmlNote };
            }

            return new XmlNote[] { xmlNote };
        }

        private static void AddBassText()
        {
            // Add chord text for left hand (e.g. Cmol)
            //if (leftTones.Contains(tone) && tone.ChordTones.Count > 0)
            //{
            //    string bassText = "_";
            //    if (tone.ChordTones.Count == 0)
            //    {
            //        bassText = note.GetPitchString();
            //    }
            //    else if (tone.ChordTones.Count == 2)
            //    {
            //        Pitch firstPitch = tone.Pitch;
            //        Pitch secondPitch = tone.ChordTones[0].Pitch;
            //        Pitch thirdPitch = tone.ChordTones[1].Pitch;

            //        List<PitchIntegerPair> pairs = new List<PitchIntegerPair>()
            //        {
            //            new PitchIntegerPair(firstPitch),
            //            new PitchIntegerPair(secondPitch),
            //            new PitchIntegerPair(thirdPitch),
            //        }.OrderBy(pair => pair.Integer).ToList();

            //        int delta1 = pairs[1].Integer - pairs[0].Integer;
            //        int delta2 = pairs[2].Integer - pairs[1].Integer;

            //        Pitch pitch = Pitch.A;
            //        AcchordType acchordType = AcchordType.SeptMin;

            //        // Dur
            //        if (delta1 == 4 && delta2 == 3)
            //        {
            //            pitch = pairs[0].Pitch; // correct
            //            acchordType = AcchordType.Dur;
            //        }
            //        else if (delta1 == 3 && delta2 == 5)
            //        {
            //            pitch = pairs[2].Pitch;
            //            acchordType = AcchordType.Dur;
            //        }
            //        else if (delta1 == 5 && delta2 == 4)
            //        {
            //            pitch = pairs[1].Pitch;
            //            acchordType = AcchordType.Dur;
            //        }
            //        // Mol
            //        else if (delta1 == 3 && delta2 == 4)
            //        {
            //            pitch = pairs[0].Pitch;
            //            acchordType = AcchordType.Mol;
            //        }
            //        else if (delta1 == 4 && delta2 == 5)
            //        {
            //            pitch = pairs[2].Pitch;
            //            acchordType = AcchordType.Mol;
            //        }
            //        else if (delta1 == 5 && delta2 == 3)
            //        {
            //            pitch = pairs[1].Pitch; // Correct
            //            acchordType = AcchordType.Mol;
            //        }

            //        bassText = $"{pitch}{acchordType}";
            //    }

            //    // Add bassText as "Lyric"
            //    note.Lyric.Add(new Lyric()
            //    {
            //        Text = new TextElementData()
            //        {
            //            Value = bassText,
            //            FontSize = "10"
            //        }
            //    });
            //}
        }


        /// <summary>
        ///   Ignores dotting
        /// </summary>
        private static XmlNote CopyXmlNote(XmlNote other)
        {
            XmlNote note = new XmlNote()
            {
                Chord = other.Chord,
                Pitch = other.Pitch,
                Duration = other.Duration,
                Type = other.Type,
                Stem = other.Stem,
                Rest = other.Rest
            }; 

            // Tiing
            if (other.Notations.Count > 0)
            {
                note.Notations.Add(other.Notations.First());
            }
            return note;
        }

        private static Attributes CreateAttributes(HandType hand, int beatType)
        {
            if (hand == HandType.Both)
            {
                throw new Exception("Hand must be left or right.");
            }

            Attributes attributes = new Attributes()
            {
                Divisions = FourDivisions,
                DivisionsSpecified = true
            };
            // Clef (= Notenschlüssel)
            attributes.Clef.Add(new Clef()
            {
                Sign = hand == HandType.Left ? ClefSign.F : ClefSign.G,
                Line = hand == HandType.Left ? "4" : "2"
            });
            // 4/4 Takt
            Time time = new Time();
            time.Beats.Add(beatType.ToString());
            time.BeatType.Add(beatType.ToString());
            attributes.Time.Add(time);
            // 5th?
            //attributes.Key.Add(new Key() { Fifths = "0" });

            return attributes;
        }

        private static ScorePartwise CreateScorePartwise(string title, double beatDuration, out ScorePartwisePart leftHandsPart, out ScorePartwisePart rightHandsPart)
        {
            const string LeftHand = "LeftHand";
            const string RightHand = "RightHand";

            // Create ScorePartwise
            ScorePartwise scorePartwise = new ScorePartwise()
            {
                MovementTitle = title,
                PartList = new PartList()
                {
                    ScorePart = new System.Collections.ObjectModel.Collection<ScorePart>()
                    {
                        new ScorePart() { Id = RightHand, PartName = new PartName() {Value = RightHand } },
                        new ScorePart() { Id = LeftHand, PartName = new PartName() {Value = LeftHand } }
                    }
                }
            };

            // Add Parts
            rightHandsPart = new ScorePartwisePart() { Id = RightHand };
            leftHandsPart = new ScorePartwisePart() { Id = LeftHand };
            scorePartwise.Part.Add(rightHandsPart);
            scorePartwise.Part.Add(leftHandsPart);

            // Add first beat
            ScorePartwisePartMeasure rightPartMeasure = new ScorePartwisePartMeasure()
            {
                Number = "1",
                Width = 192
            };
            ScorePartwisePartMeasure leftPartMeasure = new ScorePartwisePartMeasure()
            {
                Number = "1",
                Width = 192
            };

            rightHandsPart.Measure.Add(rightPartMeasure);
            leftHandsPart.Measure.Add(leftPartMeasure);

            // Add attributes
            int beatType = 4;
            leftHandsPart.Measure[0].Attributes.Add(CreateAttributes(HandType.Left, beatType));
            rightHandsPart.Measure[0].Attributes.Add(CreateAttributes(HandType.Right, beatType));

            // Set tempo
            Direction direction = new Direction()
            {
                Sound = new Sound()
                {
                    Tempo = (decimal)Math.Round(60 * beatType / beatDuration, 1),
                    TempoSpecified = true
                },
                //Directive = YesNo.Yes,
                //Placement = AboveBelow.Above,
            };
            direction.DirectionType.Add(new DirectionType());
            direction.DirectionType.First().Words.Add(new FormattedTextId()
            {
                Lang = "null"
            });

            leftHandsPart.Measure[0].Direction.Add(direction);
            rightHandsPart.Measure[0].Direction.Add(direction);

            return scorePartwise;
        }

        public void SaveAsFile(string path)
        {
            File.WriteAllText(path, "");
            XmlSerializer serializer = new XmlSerializer(typeof(ScorePartwise));

            // Using string and WriteAllText
            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, scorePartwise);
                string scoreAsString = textWriter.ToString();

                // Insert Backups (replace placeholders)
                StringBuilder builder = new StringBuilder(scoreAsString);
                IEnumerable<int> backupIndexes = scoreAsString.AllIndexesOf($"<footnote>{BackupFootnote}</footnote>").Reverse(); // Reverse so that removing text has no impact
                foreach (int index in backupIndexes)
                {
                    // Get duration
                    int durationEndIndex = scoreAsString.FindIndexBefore(index, "</duration>");
                    int durationStartIndex = scoreAsString.FindIndexBefore(index, "<duration>", true);
                    string durationString = builder.ToString(durationStartIndex, durationEndIndex - durationStartIndex);
                    int duration = Convert.ToInt32(durationString); // Make sure we have a double here
                    // Get start and end index to replace
                    int startIndex = scoreAsString.FindIndexBefore(index, "<note>");
                    int endIndex = scoreAsString.FindIndexAfter(index, "</note>", true);
                    int length = endIndex - startIndex;
                    string toRemove = builder.ToString(startIndex, length);
                    // Remove Backup-placeholder and insert actual Backup
                    builder.Remove(startIndex, length);
                    builder.Insert(startIndex, 
                          "<backup>\n"
                        + "         <duration>" + duration + "</duration>\n" // MUST NOT contain dot & decimals
                        + "      </backup>");
                }

                // Set attributes to beginning
                scoreAsString = builder.ToString();
                List<int> attributeIndexes = scoreAsString.AllIndexesOf("<direction>").Reverse().ToList(); // Attributes is always followed by Direction
                foreach (int startIndex in attributeIndexes)
                {
                    int endIndex = scoreAsString.FindIndexAfter(startIndex, "</attributes>", true);
                    string attributeText = builder.Cut(startIndex, endIndex - startIndex);
                    int measureStartIndex = scoreAsString.FindIndexBefore(startIndex, "measure");
                    int measureEndIndex = scoreAsString.FindIndexAfter(measureStartIndex, ">", true);
                    builder.Insert(measureEndIndex, "\n      " + attributeText);
                }

                // Insert LineBreaks before Notes and Backups
                builder.Replace("      <note>", "\n      <note>");
                builder.Replace("      <backup>", "\n      <backup>");

                // Remove encoding for MuseScore
                builder.Replace(" encoding=\"utf-16\"", "");

                // Write to file
                scoreAsString = builder.ToString();
                File.WriteAllText(path, scoreAsString);
            }

            // Using FileStream
            //serializer.Serialize(new FileStream(path, FileMode.OpenOrCreate), scorePartwise);

            Console.WriteLine("Saved Music Xml here as " + path);
        }
    }


    /// <summary>
    ///   Presents a NoteLength by NoteTypeValue + Dotting.
    ///   This is a approximation of a time in seconds, thats why deviation exists giving the deviation from origin time
    /// </summary>
    public class NoteLength
    {
        public NoteTypeValue NoteTypeValue { set;  get; }

        public Dotting Dotting { set; get; }

        public double ActualPortion { get; }

        /// <summary>
        ///   Always smaller than actual portion
        /// </summary>
        public double ProposedPortion { get; }

        public bool HasDeviation => Deviation > 0.001;

        //public double Deviation => MakeItHarderDict[Dotting] * Helper.GetPercentageDistance(ActualPortion, ProposedPortion);
        public double Deviation => Helper.GetPercentageDistance(ActualPortion, ProposedPortion);

        private static Dictionary<Dotting, double> MakeItHarderDict = new Dictionary<Dotting, double>()
        {
            [Dotting.None] = 1,
            [Dotting.Dotted] = 1,
            [Dotting.DoubleDotted] = 3,

            //[Dotting.Plus16th] = 3,
            //[Dotting.PlusEighth] = 2,
            //[Dotting.Plus3Eighth] = 3,
            //[Dotting.PlusQuarter] = 2,

            //[Dotting.DottedPlus16th] = 3,
            //[Dotting.DottedPlusEight] = 3,

            //[Dotting.DoubleDottedPlus16th] = 4,
            //[Dotting.DoubleDottedPlusEight] = 4,
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
            if (Dotting.HasFlag(Dotting.Dotted))
            {
                dottingString += "•";
            }
            else if (Dotting.HasFlag(Dotting.DoubleDotted))
            {
                dottingString += "••";
            }

            //if (Dotting.HasFlag(Dotting.PlusQuarter))
            //{
            //    dottingString += "¼";
            //}
            //else if (Dotting.HasFlag(Dotting.PlusEighth))
            //{
            //    dottingString += "+1/8";
            //}
            //else if (Dotting.HasFlag(Dotting.Plus16th))
            //{
            //    dottingString += "+1/16";
            //}
            //else if (Dotting.HasFlag(Dotting.Plus3Eighth))
            //{
            //    dottingString += "+3/8";
            //}
            //else if (Dotting.HasFlag(Dotting.Plus3x16th))
            //{
            //    dottingString += "+3/16";
            //}

            string deviationString = HasDeviation ? $"🗲{ProposedPortion.ToString(3)} statt {ActualPortion.ToString(3)})" : "✔";

            return $"{NoteTypeValue}{dottingString}[{deviationString}]";
        }

        // Static section
        //////////////////
        
        public static NoteLength CreateFromPortion(double portion)
        {
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

                //AddNoteLength(Dotting.Plus16th);
                //AddNoteLength(Dotting.PlusEighth);
                //AddNoteLength(Dotting.Plus3x16th);
                //AddNoteLength(Dotting.PlusQuarter);
                //AddNoteLength(Dotting.Plus3Eighth);

                //AddNoteLength(Dotting.DottedPlus16th);
                //AddNoteLength(Dotting.DottedPlusEight);
                //AddNoteLength(Dotting.DottedPlus3x16th);
                //AddNoteLength(Dotting.DottedPlus3Eight);

                //AddNoteLength(Dotting.DoubleDottedPlus16th);
                //AddNoteLength(Dotting.DoubleDottedPlusEight);
            }

            // Get result with lowest deviation
            noteLengths = noteLengths.Where(length => length.ProposedPortion < portion).OrderBy(res => res.Deviation).ToList();
            NoteLength winner = noteLengths.First();
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

            //[Dotting.Plus16th] = 1.0625,
            //[Dotting.PlusEighth] = 1.125,
            //[Dotting.Plus3x16th] = 1.1875,
            //[Dotting.PlusQuarter] = 1.25,
            //[Dotting.Plus3Eighth] = 1.375,

            //[Dotting.DottedPlus16th] = 1.5625,
            //[Dotting.DottedPlusEight] = 1.625,
            //[Dotting.DottedPlus3x16th] = 1.6875,
            //[Dotting.DottedPlus3Eight] = 1.84375,

            //[Dotting.DoubleDottedPlus16th] = 1.8125,
            //[Dotting.DoubleDottedPlusEight] = 1.875,
        };
    }

    [Flags]
    public enum Dotting
    {
        None = 0,
        Dotted = 128,
        DoubleDotted = 256,

        //Plus16th = 1,
        //PlusEighth = 2,
        //Plus3x16th = 8,
        //PlusQuarter = 4,
        //Plus3Eighth = 16,

        //DottedPlus16th = Dotted | Plus16th,
        //DottedPlusEight = Dotted | PlusEighth,
        //DottedPlus3x16th = Dotted | Plus3x16th,
        //DottedPlus3Eight = Dotted | Plus3Eighth,

        //DoubleDottedPlus16th = DoubleDotted | Plus16th,
        //DoubleDottedPlusEight = DoubleDotted | PlusEighth,
    }
}
